using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Job;
using LiventaTransfer.Application.DTOs.JobNote;
using LiventaTransfer.Application.DTOs.JobStatusHistory;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class JobService
{
    private readonly IAppDbContext _db;
    public JobService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<JobListDto>>> GetPagedAsync(
        PagedQuery query, JobStatus? status, Guid? customerId, Guid? driverId,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Jobs.AsNoTracking()
            .Include(j => j.Customer)
            .Include(j => j.Passenger)
            .Include(j => j.Driver)
            .Include(j => j.PickupLocation)
            .Include(j => j.DropoffLocation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(j => j.JobNumber.ToLower().Contains(query.Search.ToLower())
                          || j.Customer.Name.ToLower().Contains(query.Search.ToLower()));

        if (status.HasValue)
            q = q.Where(j => j.Status == status.Value);

        if (customerId.HasValue)
            q = q.Where(j => j.CustomerId == customerId.Value);

        if (driverId.HasValue)
            q = q.Where(j => j.DriverId == driverId.Value);

        if (dateFrom.HasValue)
            q = q.Where(j => j.JobDate >= dateFrom.Value);

        if (dateTo.HasValue)
            q = q.Where(j => j.JobDate <= dateTo.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "jobnumber" => query.SortDesc ? q.OrderByDescending(j => j.JobNumber) : q.OrderBy(j => j.JobNumber),
            "jobdate" => query.SortDesc ? q.OrderByDescending(j => j.JobDate) : q.OrderBy(j => j.JobDate),
            "customer" => query.SortDesc ? q.OrderByDescending(j => j.Customer.Name) : q.OrderBy(j => j.Customer.Name),
            _ => q.OrderByDescending(j => j.JobDate).ThenByDescending(j => j.JobTime)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => JobListDto.FromEntity(j))
            .ToListAsync(ct);

        return ApiResult<PagedResult<JobListDto>>.Ok(new PagedResult<JobListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "İşler listelendi.");
    }

    public async Task<ApiResult<JobDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Jobs.AsNoTracking()
            .Include(j => j.Customer)
            .Include(j => j.Passenger)
            .Include(j => j.PickupLocation)
            .Include(j => j.DropoffLocation)
            .Include(j => j.VehicleOwner)
            .Include(j => j.Vehicle)
            .Include(j => j.Driver)
            .Include(j => j.CreatedByUser)
            .Include(j => j.AssignedByUser)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (entity is null)
            return ApiResult<JobDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

        return ApiResult<JobDetailDto>.Ok(JobDetailDto.FromEntity(entity), "İş bulundu.");
    }

    public async Task<ApiResult<JobDetailDto>> CreateAsync(CreateJobRequest request, Guid userId, CancellationToken ct)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
            return ApiResult<JobDetailDto>.Fail("Müşteri bulunamadı.", statusCode: 400);

        var jobNumber = await GenerateJobNumberAsync(ct);

        var entity = new Domain.Entities.Job
        {
            JobNumber = jobNumber,
            JobDate = request.JobDate,
            JobTime = request.JobTime,
            JobType = request.JobType,
            Status = JobStatus.Open,
            CustomerId = request.CustomerId,
            PassengerId = request.PassengerId,
            PassengerCount = request.PassengerCount,
            PickupLocationId = request.PickupLocationId,
            DropoffLocationId = request.DropoffLocationId,
            PickupAddress = request.PickupAddress?.Trim(),
            DropoffAddress = request.DropoffAddress?.Trim(),
            RouteDescription = request.RouteDescription?.Trim(),
            FlightCode = request.FlightCode?.Trim(),
            ExtraInfo = request.ExtraInfo?.Trim(),
            Notes = request.Notes?.Trim(),
            SourceEmail = request.SourceEmail?.Trim(),
            VehicleOwnerId = request.VehicleOwnerId,
            VehicleId = request.VehicleId,
            DriverId = request.DriverId,
            SalePrice = request.SalePrice,
            PurchasePrice = request.PurchasePrice,
            ExtraCost = request.ExtraCost,
            CreatedByUserId = userId
        };

        _db.Jobs.Add(entity);

        _db.JobStatusHistories.Add(new Domain.Entities.JobStatusHistory
        {
            JobId = entity.Id,
            OldStatus = null,
            NewStatus = JobStatus.Open,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<JobDetailDto>> UpdateAsync(Guid id, UpdateJobRequest request, CancellationToken ct)
    {
        var entity = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null)
            return ApiResult<JobDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

        if (!await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
            return ApiResult<JobDetailDto>.Fail("Müşteri bulunamadı.", statusCode: 400);

        entity.JobDate = request.JobDate;
        entity.JobTime = request.JobTime;
        entity.JobType = request.JobType;
        entity.CustomerId = request.CustomerId;
        entity.PassengerId = request.PassengerId;
        entity.PassengerCount = request.PassengerCount;
        entity.PickupLocationId = request.PickupLocationId;
        entity.DropoffLocationId = request.DropoffLocationId;
        entity.PickupAddress = request.PickupAddress?.Trim();
        entity.DropoffAddress = request.DropoffAddress?.Trim();
        entity.RouteDescription = request.RouteDescription?.Trim();
        entity.FlightCode = request.FlightCode?.Trim();
        entity.ExtraInfo = request.ExtraInfo?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.SourceEmail = request.SourceEmail?.Trim();
        entity.VehicleOwnerId = request.VehicleOwnerId;
        entity.VehicleId = request.VehicleId;
        entity.DriverId = request.DriverId;
        entity.SalePrice = request.SalePrice;
        entity.PurchasePrice = request.PurchasePrice;
        entity.ExtraCost = request.ExtraCost;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("İş bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "İş silindi.");
    }

    public async Task<ApiResult<JobDetailDto>> UpdateStatusAsync(Guid id, UpdateJobStatusRequest request, Guid userId, CancellationToken ct)
    {
        var entity = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null)
            return ApiResult<JobDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

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

    public async Task<ApiResult<List<JobStatusHistoryDto>>> GetStatusHistoryAsync(Guid jobId, CancellationToken ct)
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

    private async Task<string> GenerateJobNumberAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var prefix = $"JOB-{today:yyyyMMdd}";
        var count = await _db.Jobs.CountAsync(j => j.JobNumber.StartsWith(prefix), ct);
        return $"{prefix}-{(count + 1):D4}";
    }
}
