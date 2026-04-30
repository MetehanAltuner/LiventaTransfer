using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Job;
using LiventaTransfer.Application.DTOs.JobStatusHistory;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Entities;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class JobService
{
    private static readonly JobStatus[] MergeableStatuses = [JobStatus.Open, JobStatus.Assigned];

    private readonly IAppDbContext _db;
    public JobService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<JobListDto>>> GetPagedAsync(
        PagedQuery query, JobStatus? status, long? customerId, long? driverId, long? locationId,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Jobs.AsNoTracking()
            .Include(j => j.Driver)
            .Include(j => j.Stops).ThenInclude(s => s.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            q = q.Where(j => j.JobNumber.ToLower().Contains(search)
                          || j.Stops.Any(s => s.Customer.Name.ToLower().Contains(search)));
        }

        if (status.HasValue)
            q = q.Where(j => j.Status == status.Value);

        if (customerId.HasValue)
            q = q.Where(j => j.Stops.Any(s => s.CustomerId == customerId.Value));

        if (driverId.HasValue)
            q = q.Where(j => j.DriverId == driverId.Value);

        if (locationId.HasValue)
            q = q.Where(j => j.Stops.Any(s =>
                s.PickupLocationId == locationId.Value ||
                s.DropoffLocationId == locationId.Value));

        if (dateFrom.HasValue)
            q = q.Where(j => j.JobDate >= dateFrom.Value);

        if (dateTo.HasValue)
            q = q.Where(j => j.JobDate <= dateTo.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "jobnumber" => query.SortDesc ? q.OrderByDescending(j => j.JobNumber) : q.OrderBy(j => j.JobNumber),
            "jobdate" => query.SortDesc ? q.OrderByDescending(j => j.JobDate) : q.OrderBy(j => j.JobDate),
            _ => q.OrderByDescending(j => j.JobDate).ThenByDescending(j => j.JobTime)
        };

        var entities = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = entities.Select(JobListDto.FromEntity).ToList();

        return ApiResult<PagedResult<JobListDto>>.Ok(new PagedResult<JobListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "İşler listelendi.");
    }

    public async Task<ApiResult<JobDetailDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var entity = await LoadDetailAsync(id, asNoTracking: true, ct);
        if (entity is null)
            return ApiResult<JobDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

        return ApiResult<JobDetailDto>.Ok(JobDetailDto.FromEntity(entity), "İş bulundu.");
    }

    public async Task<ApiResult<JobDetailDto>> CreateAsync(CreateJobRequest request, Guid userId, CancellationToken ct)
    {
        if (request.Stops is null || request.Stops.Count == 0)
            return ApiResult<JobDetailDto>.Fail("En az bir durak (stop) gereklidir.", statusCode: 400);

        var validation = await ValidateStopsAsync(request.Stops, ct);
        if (validation is not null)
            return ApiResult<JobDetailDto>.Fail(validation, statusCode: 400);

        var jobNumber = await GenerateJobNumberAsync(ct);

        var entity = new Domain.Entities.Job
        {
            JobNumber = jobNumber,
            JobDate = request.JobDate,
            JobTime = request.JobTime,
            JobType = request.JobType,
            Status = JobStatus.Open,
            RouteDescription = request.RouteDescription?.Trim(),
            ExtraInfo = request.ExtraInfo?.Trim(),
            Notes = request.Notes?.Trim(),
            SourceEmail = request.SourceEmail?.Trim(),
            VehicleOwnerId = request.VehicleOwnerId,
            VehicleId = request.VehicleId,
            DriverId = request.DriverId,
            PurchasePrice = request.PurchasePrice,
            ExtraCost = request.ExtraCost,
            CreatedByUserId = userId
        };

        var seq = 1;
        foreach (var s in request.Stops)
            entity.Stops.Add(BuildStop(s, seq++));

        _db.Jobs.Add(entity);

        _db.JobStatusHistories.Add(new Domain.Entities.JobStatusHistory
        {
            Job = entity,
            OldStatus = null,
            NewStatus = JobStatus.Open,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<JobDetailDto>> UpdateAsync(long id, UpdateJobRequest request, CancellationToken ct)
    {
        if (request.Stops is null || request.Stops.Count == 0)
            return ApiResult<JobDetailDto>.Fail("En az bir durak (stop) gereklidir.", statusCode: 400);

        var entity = await _db.Jobs
            .Include(j => j.Stops)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null)
            return ApiResult<JobDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

        if (entity.Status == JobStatus.Merged)
            return ApiResult<JobDetailDto>.Fail("Birleştirilmiş iş güncellenemez.", statusCode: 400);

        var validation = await ValidateStopsAsync(request.Stops, ct);
        if (validation is not null)
            return ApiResult<JobDetailDto>.Fail(validation, statusCode: 400);

        entity.JobDate = request.JobDate;
        entity.JobTime = request.JobTime;
        entity.JobType = request.JobType;
        entity.RouteDescription = request.RouteDescription?.Trim();
        entity.ExtraInfo = request.ExtraInfo?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.SourceEmail = request.SourceEmail?.Trim();
        entity.VehicleOwnerId = request.VehicleOwnerId;
        entity.VehicleId = request.VehicleId;
        entity.DriverId = request.DriverId;
        entity.PurchasePrice = request.PurchasePrice;
        entity.ExtraCost = request.ExtraCost;

        _db.JobStops.RemoveRange(entity.Stops);
        entity.Stops.Clear();

        var seq = 1;
        foreach (var s in request.Stops)
            entity.Stops.Add(BuildStop(s, seq++));

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("İş bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "İş silindi.");
    }

    public async Task<ApiResult<JobDetailDto>> UpdateStatusAsync(long id, UpdateJobStatusRequest request, Guid userId, CancellationToken ct)
    {
        var entity = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null)
            return ApiResult<JobDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

        if (entity.Status == JobStatus.Merged)
            return ApiResult<JobDetailDto>.Fail("Birleştirilmiş işin durumu değiştirilemez.", statusCode: 400);

        if (request.NewStatus == JobStatus.Merged)
            return ApiResult<JobDetailDto>.Fail("'Birleştirildi' durumu yalnızca birleştirme işlemi ile atanabilir.", statusCode: 400);

        var oldStatus = entity.Status;
        entity.Status = request.NewStatus;

        if (request.NewStatus == JobStatus.Assigned && entity.DriverId.HasValue)
            entity.AssignedByUserId = userId;

        _db.JobStatusHistories.Add(new Domain.Entities.JobStatusHistory
        {
            JobId = entity.Id,
            OldStatus = oldStatus,
            NewStatus = request.NewStatus,
            ChangedByUserId = userId,
            ChangeReason = request.ChangeReason?.Trim(),
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<JobDetailDto>> MergeAsync(long targetId, MergeJobsRequest request, Guid userId, CancellationToken ct)
    {
        var sourceIds = request.SourceJobIds?.Distinct().ToList() ?? [];
        if (sourceIds.Count == 0)
            return ApiResult<JobDetailDto>.Fail("En az bir kaynak iş seçilmelidir.", statusCode: 400);

        if (sourceIds.Contains(targetId))
            return ApiResult<JobDetailDto>.Fail("Hedef iş kaynak listesinde olamaz.", statusCode: 400);

        var target = await _db.Jobs
            .Include(j => j.Stops)
            .FirstOrDefaultAsync(j => j.Id == targetId, ct);
        if (target is null)
            return ApiResult<JobDetailDto>.Fail("Hedef iş bulunamadı.", statusCode: 404);

        if (!MergeableStatuses.Contains(target.Status))
            return ApiResult<JobDetailDto>.Fail(
                $"Hedef iş yalnızca {string.Join(" / ", MergeableStatuses.Select(EnumLabelHelper.GetLabel))} durumunda birleştirilebilir.",
                statusCode: 400);

        var sources = await _db.Jobs
            .Include(j => j.Stops)
            .Where(j => sourceIds.Contains(j.Id))
            .ToListAsync(ct);

        var foundIds = sources.Select(s => s.Id).ToHashSet();
        var missing = sourceIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missing.Count > 0)
            return ApiResult<JobDetailDto>.Fail(
                $"Kaynak iş bulunamadı: {string.Join(", ", missing)}", statusCode: 404);

        var blocked = sources.Where(s => !MergeableStatuses.Contains(s.Status)).ToList();
        if (blocked.Count > 0)
            return ApiResult<JobDetailDto>.Fail(
                $"Şu işler birleştirilemez (durum uygun değil): {string.Join(", ", blocked.Select(b => b.JobNumber))}",
                statusCode: 400);

        var nextSeq = (target.Stops.Count == 0 ? 0 : target.Stops.Max(s => s.Sequence)) + 1;
        var now = DateTime.UtcNow;

        foreach (var source in sources)
        {
            foreach (var stop in source.Stops.OrderBy(s => s.Sequence))
            {
                stop.JobId = target.Id;
                stop.Sequence = nextSeq++;
            }

            var oldStatus = source.Status;
            source.Status = JobStatus.Merged;
            source.MergedIntoJobId = target.Id;

            _db.JobStatusHistories.Add(new Domain.Entities.JobStatusHistory
            {
                JobId = source.Id,
                OldStatus = oldStatus,
                NewStatus = JobStatus.Merged,
                ChangedByUserId = userId,
                ChangeReason = $"{target.JobNumber} ile birleştirildi.",
                ChangedAt = now
            });
        }

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(target.Id, ct);
    }

    public async Task<ApiResult<List<JobStatusHistoryDto>>> GetStatusHistoryAsync(long jobId, CancellationToken ct)
    {
        if (!await _db.Jobs.AnyAsync(j => j.Id == jobId, ct))
            return ApiResult<List<JobStatusHistoryDto>>.Fail("İş bulunamadı.", statusCode: 404);

        var items = await _db.JobStatusHistories
            .AsNoTracking()
            .Include(h => h.ChangedByUser)
            .Where(h => h.JobId == jobId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => JobStatusHistoryDto.FromEntity(h))
            .ToListAsync(ct);

        return ApiResult<List<JobStatusHistoryDto>>.Ok(items, "Durum geçmişi listelendi.");
    }

    private async Task<Domain.Entities.Job?> LoadDetailAsync(long id, bool asNoTracking, CancellationToken ct)
    {
        var q = _db.Jobs.AsQueryable();
        if (asNoTracking) q = q.AsNoTracking();

        return await q
            .Include(j => j.VehicleOwner)
            .Include(j => j.Vehicle)
            .Include(j => j.Driver)
            .Include(j => j.CreatedByUser)
            .Include(j => j.AssignedByUser)
            .Include(j => j.MergedIntoJob)
            .Include(j => j.MergedJobs)
            .Include(j => j.Stops).ThenInclude(s => s.Customer)
            .Include(j => j.Stops).ThenInclude(s => s.Passenger)
            .Include(j => j.Stops).ThenInclude(s => s.PickupLocation)
            .Include(j => j.Stops).ThenInclude(s => s.DropoffLocation)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
    }

    private async Task<string?> ValidateStopsAsync(List<JobStopRequest> stops, CancellationToken ct)
    {
        var customerIds = stops.Select(s => s.CustomerId).Distinct().ToList();
        var existingCustomerIds = await _db.Customers
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(ct);
        var missingCustomers = customerIds.Except(existingCustomerIds).ToList();
        if (missingCustomers.Count > 0)
            return $"Müşteri bulunamadı: {string.Join(", ", missingCustomers)}";

        var locationIds = stops
            .SelectMany(s => new[] { s.PickupLocationId, s.DropoffLocationId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        if (locationIds.Count > 0)
        {
            var existingLocationIds = await _db.Locations
                .Where(l => locationIds.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync(ct);
            var missingLocations = locationIds.Except(existingLocationIds).ToList();
            if (missingLocations.Count > 0)
                return $"Lokasyon bulunamadı: {string.Join(", ", missingLocations)}";
        }

        var passengerIds = stops
            .Where(s => s.PassengerId.HasValue)
            .Select(s => s.PassengerId!.Value)
            .Distinct()
            .ToList();
        if (passengerIds.Count > 0)
        {
            var existingPassengerIds = await _db.Passengers
                .Where(p => passengerIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(ct);
            var missingPassengers = passengerIds.Except(existingPassengerIds).ToList();
            if (missingPassengers.Count > 0)
                return $"Yolcu bulunamadı: {string.Join(", ", missingPassengers)}";
        }

        return null;
    }

    private static JobStop BuildStop(JobStopRequest s, int sequence) => new()
    {
        Sequence = sequence,
        CustomerId = s.CustomerId,
        PassengerId = s.PassengerId,
        PassengerCount = s.PassengerCount <= 0 ? 1 : s.PassengerCount,
        PickupLocationId = s.PickupLocationId,
        DropoffLocationId = s.DropoffLocationId,
        PickupAddress = s.PickupAddress?.Trim(),
        DropoffAddress = s.DropoffAddress?.Trim(),
        FlightCode = s.FlightCode?.Trim(),
        Notes = s.Notes?.Trim(),
        SalePrice = s.SalePrice
    };

    private async Task<string> GenerateJobNumberAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var prefix = $"JOB-{today:yyyyMMdd}";
        var count = await _db.Jobs.CountAsync(j => j.JobNumber.StartsWith(prefix), ct);
        return $"{prefix}-{(count + 1):D4}";
    }
}
