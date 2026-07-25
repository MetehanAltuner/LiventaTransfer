using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Job;
using LiventaTransfer.Application.DTOs.JobStatusHistory;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Application.Interfaces.Services;
using LiventaTransfer.Domain.Entities;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class JobService
{
    private static readonly JobStatus[] MergeableStatuses = [JobStatus.Open];

    // Müşteriye özel iç/dış hat birleştirme kuralları için sabit müşteri id'leri.
    private const long HavelsanCustomerId = 10;
    private const long RoketsanCustomerId = 14;

    // Roketsan kuralı: dış hat işi, iç hat işinden en az bu kadar dakika önce olmalı.
    private const int RoketsanMinLeadMinutes = 30;

    private readonly IAppDbContext _db;
    private readonly IJobBroadcaster _broadcaster;

    public JobService(IAppDbContext db, IJobBroadcaster broadcaster)
    {
        _db = db;
        _broadcaster = broadcaster;
    }

    public async Task<ApiResult<PagedResult<JobListDto>>> GetPagedAsync(
        PagedQuery query, JobStatus? status, long? customerId, long? driverId, long? locationId,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Jobs.AsNoTracking()
            .Include(j => j.Contractor)
            .Include(j => j.Driver)
            .Include(j => j.Stops).ThenInclude(s => s.Customer)
            .Include(j => j.Stops).ThenInclude(s => s.Passengers).ThenInclude(p => p.Passenger)
            .Include(j => j.Stops).ThenInclude(s => s.PickupLocation)
            .Include(j => j.Stops).ThenInclude(s => s.DropoffLocation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ToLower her iki tarafa uygulanarak büyük/küçük harf duyarsız eşleşme sağlanır
            var search = query.Search.Trim().ToLower();
            q = q.Where(j => j.JobNumber.ToLower().Contains(search)
                          || (j.Driver != null && j.Driver.FullName.ToLower().Contains(search))
                          || j.Stops.Any(s => s.Customer.Name.ToLower().Contains(search))
                          || j.Stops.Any(s => s.Passengers.Any(p =>
                                p.Passenger.FullName.ToLower().Contains(search))));
        }

        if (status.HasValue)
            q = q.Where(j => j.Status == status.Value);
        else
            q = q.Where(j => j.Status != JobStatus.Merged);

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

    /// <summary>
    /// İlgili duraktaki bir yolcuya transfer bilgisinin gönderildiğini işaretler (yolcu bazında).
    /// ContactedAt'tan bağımsızdır. Idempotent: zaten işaretliyse mevcut zaman korunur.
    /// </summary>
    public async Task<ApiResult<JobDetailDto>> MarkStopInfoSentAsync(long jobId, long stopId, long passengerId, CancellationToken ct)
    {
        var entity = await _db.Jobs
            .Include(j => j.Stops).ThenInclude(s => s.Passengers)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (entity is null)
            return ApiResult<JobDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

        var stop = entity.Stops.FirstOrDefault(s => s.Id == stopId);
        if (stop is null)
            return ApiResult<JobDetailDto>.Fail("Durak bu işe ait değil.", statusCode: 404);

        var stopPassenger = stop.Passengers.FirstOrDefault(p => p.PassengerId == passengerId);
        if (stopPassenger is null)
            return ApiResult<JobDetailDto>.Fail("Yolcu bu durağa ait değil.", statusCode: 404);

        if (!stopPassenger.InfoSentAt.HasValue)
        {
            stopPassenger.InfoSentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await _broadcaster.BroadcastJobsChangedAsync(ct);
        }

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<JobDetailDto>> CreateAsync(CreateJobRequest request, Guid userId, CancellationToken ct)
    {
        if (request.Stops is null || request.Stops.Count == 0)
            return ApiResult<JobDetailDto>.Fail("En az bir durak (stop) gereklidir.", statusCode: 400);

        var userValidation = await ValidateUserAsync(userId, ct);
        if (userValidation is not null)
            return ApiResult<JobDetailDto>.Fail(userValidation, statusCode: 400);

        var refValidation = await ValidateJobReferencesAsync(request.ContractorId, request.VehicleOwnerId, request.VehicleId, request.DriverId, ct);
        if (refValidation is not null)
            return ApiResult<JobDetailDto>.Fail(refValidation, statusCode: 400);

        var validation = await ValidateStopsAsync(request.Stops, ct);
        if (validation is not null)
            return ApiResult<JobDetailDto>.Fail(validation, statusCode: 400);

        var resolvedStops = JobStopSequenceHelper.OrderByRequestedSequence(
            await ResolveStopLocationsAsync(request.Stops, ct));

        var jobNumber = await GenerateJobNumberAsync(ct);

        var entity = new Domain.Entities.Job
        {
            JobNumber = jobNumber,
            JobDate = request.JobDate,
            JobTime = request.JobTime,
            JobType = request.JobType,
            FlightType = request.FlightType,
            FlightTime = request.FlightTime,
            Status = JobStatus.Open,
            RouteDescription = request.RouteDescription?.Trim(),
            ExtraInfo = request.ExtraInfo?.Trim(),
            Notes = request.Notes?.Trim(),
            SourceEmail = request.SourceEmail?.Trim(),
            ContractorId = request.ContractorId,
            VehicleOwnerId = request.VehicleOwnerId,
            VehicleId = request.VehicleId,
            DriverId = request.DriverId,
            SalePrice = request.SalePrice,
            PurchasePrice = request.PurchasePrice,
            ExtraCost = request.ExtraCost,
            CreatedByUserId = userId
        };

        var seq = 1;
        foreach (var s in resolvedStops)
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
        await _broadcaster.BroadcastJobsChangedAsync(ct);

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

        var refValidation = await ValidateJobReferencesAsync(request.ContractorId, request.VehicleOwnerId, request.VehicleId, request.DriverId, ct);
        if (refValidation is not null)
            return ApiResult<JobDetailDto>.Fail(refValidation, statusCode: 400);

        var validation = await ValidateStopsAsync(request.Stops, ct);
        if (validation is not null)
            return ApiResult<JobDetailDto>.Fail(validation, statusCode: 400);

        entity.JobDate = request.JobDate;
        entity.JobTime = request.JobTime;
        entity.JobType = request.JobType;
        entity.FlightType = request.FlightType;
        entity.FlightTime = request.FlightTime;
        entity.RouteDescription = request.RouteDescription?.Trim();
        entity.ExtraInfo = request.ExtraInfo?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.SourceEmail = request.SourceEmail?.Trim();
        entity.ContractorId = request.ContractorId;
        entity.VehicleOwnerId = request.VehicleOwnerId;
        entity.VehicleId = request.VehicleId;
        entity.DriverId = request.DriverId;
        entity.SalePrice = request.SalePrice;
        entity.PurchasePrice = request.PurchasePrice;
        entity.ExtraCost = request.ExtraCost;

        var resolvedStops = JobStopSequenceHelper.OrderByRequestedSequence(
            await ResolveStopLocationsAsync(request.Stops, ct));

        _db.JobStops.RemoveRange(entity.Stops);
        entity.Stops.Clear();

        var seq = 1;
        foreach (var s in resolvedStops)
            entity.Stops.Add(BuildStop(s, seq++));

        await _db.SaveChangesAsync(ct);
        await _broadcaster.BroadcastJobsChangedAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    /// <summary>
    /// İşin duraklarını verilen durak id + sıra numarası listesine göre yeniden sıralar.
    /// Gönderilen liste işin TÜM duraklarını kapsamalıdır; sıra numaraları 1..n olarak
    /// normalize edilerek kaydedilir.
    /// </summary>
    public async Task<ApiResult<JobDetailDto>> ReorderStopsAsync(long id, ReorderJobStopsRequest request, CancellationToken ct)
    {
        var items = request.Stops ?? [];
        if (items.Count == 0)
            return ApiResult<JobDetailDto>.Fail("En az bir durak (stop) gereklidir.", statusCode: 400);

        var entity = await _db.Jobs
            .Include(j => j.Stops)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null)
            return ApiResult<JobDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

        if (entity.Status == JobStatus.Merged)
            return ApiResult<JobDetailDto>.Fail("Birleştirilmiş işin durakları yeniden sıralanamaz.", statusCode: 400);

        var duplicateStopIds = items
            .GroupBy(i => i.StopId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateStopIds.Count > 0)
            return ApiResult<JobDetailDto>.Fail(
                $"Aynı durak (stopId) birden fazla gönderilemez: {string.Join(", ", duplicateStopIds)}", statusCode: 400);

        var duplicateSequences = items
            .GroupBy(i => i.Sequence)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateSequences.Count > 0)
            return ApiResult<JobDetailDto>.Fail(
                $"Aynı sıra numarası (sequence) birden fazla durakta kullanılamaz: {string.Join(", ", duplicateSequences)}", statusCode: 400);

        var jobStopIds = entity.Stops.Select(s => s.Id).ToHashSet();

        var unknownStopIds = items
            .Where(i => !jobStopIds.Contains(i.StopId))
            .Select(i => i.StopId)
            .ToList();
        if (unknownStopIds.Count > 0)
            return ApiResult<JobDetailDto>.Fail(
                $"Durak bu işe ait değil: {string.Join(", ", unknownStopIds)}", statusCode: 400);

        var sentStopIds = items.Select(i => i.StopId).ToHashSet();
        var missingStopIds = jobStopIds.Where(sid => !sentStopIds.Contains(sid)).ToList();
        if (missingStopIds.Count > 0)
            return ApiResult<JobDetailDto>.Fail(
                $"İşin tüm durakları gönderilmelidir. Eksik duraklar: {string.Join(", ", missingStopIds)}", statusCode: 400);

        var seq = 1;
        foreach (var item in items.OrderBy(i => i.Sequence))
            entity.Stops.First(s => s.Id == item.StopId).Sequence = seq++;

        await _db.SaveChangesAsync(ct);
        await _broadcaster.BroadcastJobsChangedAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("İş bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        await _broadcaster.BroadcastJobsChangedAsync(ct);

        return ApiResult<bool>.Ok(true, "İş silindi.");
    }

    public async Task<ApiResult<JobDetailDto>> UpdateStatusAsync(long id, UpdateJobStatusRequest request, Guid userId, CancellationToken ct)
    {
        var userValidation = await ValidateUserAsync(userId, ct);
        if (userValidation is not null)
            return ApiResult<JobDetailDto>.Fail(userValidation, statusCode: 400);

        var entity = await _db.Jobs
            .Include(j => j.Stops)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null)
            return ApiResult<JobDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

        if (entity.Status == JobStatus.Merged)
            return ApiResult<JobDetailDto>.Fail("Birleştirilmiş işin durumu değiştirilemez.", statusCode: 400);

        if (request.NewStatus == JobStatus.Merged)
            return ApiResult<JobDetailDto>.Fail("'Birleştirildi' durumu yalnızca birleştirme işlemi ile atanabilir.", statusCode: 400);

        var stageGuard = ValidateStatusAgainstDriverStage(entity, request.NewStatus);
        if (stageGuard is not null)
            return ApiResult<JobDetailDto>.Fail(stageGuard, statusCode: 400);

        var userExists = await UserExistsAsync(userId, ct);

        var oldStatus = entity.Status;
        entity.Status = request.NewStatus;

        if (request.NewStatus == JobStatus.Assigned && entity.DriverId.HasValue && userExists)
            entity.AssignedByUserId = userId;

        if (userExists)
        {
            _db.JobStatusHistories.Add(new Domain.Entities.JobStatusHistory
            {
                JobId = entity.Id,
                OldStatus = oldStatus,
                NewStatus = request.NewStatus,
                ChangedByUserId = userId,
                ChangeReason = request.ChangeReason?.Trim(),
                ChangedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        await _broadcaster.BroadcastJobsChangedAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<JobDetailDto>> MergeAsync(MergeJobsRequest request, Guid userId, CancellationToken ct)
    {
        var userValidation = await ValidateUserAsync(userId, ct);
        if (userValidation is not null)
            return ApiResult<JobDetailDto>.Fail(userValidation, statusCode: 400);

        var jobIds = request.JobIds?.Distinct().ToList() ?? [];
        if (jobIds.Count < 2)
            return ApiResult<JobDetailDto>.Fail("Birleştirme için en az iki farklı iş gereklidir.", statusCode: 400);

        var jobs = await _db.Jobs
            .Include(j => j.Stops)
            .Where(j => jobIds.Contains(j.Id))
            .ToListAsync(ct);

        var foundIds = jobs.Select(s => s.Id).ToHashSet();
        var missing = jobIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missing.Count > 0)
            return ApiResult<JobDetailDto>.Fail(
                $"İş bulunamadı: {string.Join(", ", missing)}", statusCode: 404);

        var blocked = jobs.Where(s => !MergeableStatuses.Contains(s.Status)).ToList();
        if (blocked.Count > 0)
            return ApiResult<JobDetailDto>.Fail(
                $"Yalnızca 'Açık' durumundaki işler birleştirilebilir. Uygun olmayan işler: {string.Join(", ", blocked.Select(b => b.JobNumber))}",
                statusCode: 400);

        // VIP kuralı: VIP yolcu içeren işler birleştirilemez.
        var vipJobNumbers = await _db.Jobs
            .Where(j => jobIds.Contains(j.Id)
                        && j.Stops.SelectMany(s => s.Passengers).Any(p => p.Passenger.IsVip))
            .Select(j => j.JobNumber)
            .ToListAsync(ct);
        if (vipJobNumbers.Count > 0)
            return ApiResult<JobDetailDto>.Fail(
                $"VIP yolcu içeren işler birleştirilemez: {string.Join(", ", vipJobNumbers)}",
                statusCode: 400);

        var distinctTypes = jobs.Select(j => j.JobType).Distinct().ToList();
        if (distinctTypes.Count > 1)
            return ApiResult<JobDetailDto>.Fail(
                "Farklı tiplerdeki işler birleştirilemez. Birleştirilecek işlerin tamamı aynı iş tipinde olmalıdır.",
                statusCode: 400);

        var hasDomestic = jobs.Any(j => j.FlightType == FlightType.Domestic);
        var hasInternational = jobs.Any(j => j.FlightType == FlightType.International);
        if (hasDomestic && hasInternational)
        {
            // Müşteriye özel kural yalnızca birleşen işlerin TAMAMINDA o müşteri varsa geçerli
            // (Havelsan yalnızca Havelsan ile, Roketsan yalnızca Roketsan ile). Farklı müşterilerle
            // karışan senaryolarda varsayılan iç/dış bloku uygulanır.
            var allHavelsan = jobs.All(j => j.Stops.Any(s => s.CustomerId == HavelsanCustomerId));
            var allRoketsan = jobs.All(j => j.Stops.Any(s => s.CustomerId == RoketsanCustomerId));

            if (allHavelsan)
            {
                // Havelsan: iç ve dış hat işleri koşulsuz birleştirilebilir.
            }
            else if (allRoketsan)
            {
                // Roketsan: dış hat uçuşu, iç hat uçuşundan en az yarım saat önce ise birleştirilebilir.
                // Kontrol iş üzerindeki uçuş saatinden (FlightTime) yapılır.
                var internationalJobs = jobs.Where(j => j.FlightType == FlightType.International).ToList();
                var domesticJobs = jobs.Where(j => j.FlightType == FlightType.Domestic).ToList();

                if (internationalJobs.Any(j => j.FlightTime is null) || domesticJobs.Any(j => j.FlightTime is null))
                    return ApiResult<JobDetailDto>.Fail(
                        "Uçuş saati girilmemiş işler iç/dış hat kuralına göre birleştirilemez. Lütfen ilgili işlerin uçuş saatini girin.",
                        statusCode: 400);

                var latestInternational = internationalJobs.Max(j => j.JobDate.ToDateTime(j.FlightTime!.Value));
                var earliestDomestic = domesticJobs.Min(j => j.JobDate.ToDateTime(j.FlightTime!.Value));

                if (latestInternational > earliestDomestic.AddMinutes(-RoketsanMinLeadMinutes))
                    return ApiResult<JobDetailDto>.Fail(
                        "Dış hat işi, iç hat işinden en az yarım saat önce olmalıdır.",
                        statusCode: 400);
            }
            else
            {
                return ApiResult<JobDetailDto>.Fail(
                    "İç hat ve dış hat işleri birleştirilemez.",
                    statusCode: 400);
            }
        }

        var distinctDrivers = jobs.Select(j => j.DriverId).Distinct().ToList();
        if (distinctDrivers.Count > 1)
            return ApiResult<JobDetailDto>.Fail(
                "Farklı sürücülere atanmış işler birleştirilemez. Birleştirilecek işlerin tamamı aynı sürücüye atanmış (veya hepsi atanmamış) olmalıdır.",
                statusCode: 400);

        var target = jobs
            .OrderBy(j => j.JobDate)
            .ThenBy(j => j.JobTime)
            .ThenBy(j => j.Id)
            .First();
        var sources = jobs.Where(j => j.Id != target.Id).ToList();

        var nextSeq = (target.Stops.Count == 0 ? 0 : target.Stops.Max(s => s.Sequence)) + 1;
        var now = DateTime.UtcNow;
        var userExists = await UserExistsAsync(userId, ct);

        foreach (var source in sources)
        {
            foreach (var stop in source.Stops.OrderBy(s => s.Sequence))
            {
                stop.JobId = target.Id;
                stop.Sequence = nextSeq++;
            }

            // Fiyat iş seviyesinde tutulduğundan, birleştirilen işlerin satış fiyatları
            // hedef işte toplanır (kayıpsız). İkisi de null ise değer null kalır.
            if (source.SalePrice.HasValue || target.SalePrice.HasValue)
                target.SalePrice = (target.SalePrice ?? 0) + (source.SalePrice ?? 0);

            var oldStatus = source.Status;
            source.Status = JobStatus.Merged;
            source.MergedIntoJobId = target.Id;

            if (userExists)
            {
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
        }

        await _db.SaveChangesAsync(ct);
        await _broadcaster.BroadcastJobsChangedAsync(ct);

        return await GetByIdAsync(target.Id, ct);
    }

    public async Task<ApiResult<List<DriverActiveJobDto>>> GetActiveJobsForDriverAsync(long driverId, CancellationToken ct)
    {
        var items = await _db.Jobs.AsNoTracking()
            .Where(j => j.DriverId == driverId && (int)j.Status < (int)JobStatus.Completed)
            .OrderBy(j => j.JobDate).ThenBy(j => j.JobTime)
            .Select(j => new DriverActiveJobDto { JobNumber = j.JobNumber, PublicId = j.PublicId })
            .ToListAsync(ct);

        return ApiResult<List<DriverActiveJobDto>>.Ok(items, "Sürücünün aktif işleri listelendi.");
    }

    public async Task<ApiResult<TransferDetailDto>> GetTransferDetailAsync(Guid publicId, CancellationToken ct)
    {
        var id = await ResolveJobIdAsync(publicId, ct);
        if (id is null)
            return ApiResult<TransferDetailDto>.Fail("İş bulunamadı.", statusCode: 404);
        return await GetTransferDetailByIdAsync(id.Value, ct);
    }

    private async Task<ApiResult<TransferDetailDto>> GetTransferDetailByIdAsync(long id, CancellationToken ct)
    {
        var entity = await LoadDetailAsync(id, asNoTracking: true, ct);
        if (entity is null)
            return ApiResult<TransferDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

        return ApiResult<TransferDetailDto>.Ok(TransferDetailDto.FromEntity(entity), "Transfer detayı.");
    }

    public async Task<ApiResult<TransferDetailDto>> MarkContactedAsync(Guid publicId, Guid userId, CancellationToken ct)
        => await MarkJobLevelAsync(publicId, userId, isDepart: false, ct);

    public async Task<ApiResult<TransferDetailDto>> MarkDepartedAsync(Guid publicId, Guid userId, CancellationToken ct)
        => await MarkJobLevelAsync(publicId, userId, isDepart: true, ct);

    /// <summary>
    /// Verilen userId'nin Users tablosunda gerçekten var olup olmadığını döner.
    /// JobStatusHistory ve benzeri FK kayıtlarını oluşturmadan önce kullanılır;
    /// kullanıcı yoksa kayıt atlanır ve FK ihlalinden dolayı 500 dönmez.
    /// </summary>
    private async Task<bool> UserExistsAsync(Guid userId, CancellationToken ct)
        => userId != Guid.Empty && await _db.Users.AnyAsync(u => u.Id == userId, ct);

    private async Task<long?> ResolveJobIdAsync(Guid publicId, CancellationToken ct)
        => await _db.Jobs.AsNoTracking()
            .Where(j => j.PublicId == publicId)
            .Select(j => (long?)j.Id)
            .FirstOrDefaultAsync(ct);

    private async Task<ApiResult<TransferDetailDto>> MarkJobLevelAsync(Guid publicId, Guid userId, bool isDepart, CancellationToken ct)
    {
        var entity = await _db.Jobs
            .Include(j => j.Stops)
            .FirstOrDefaultAsync(j => j.PublicId == publicId, ct);
        if (entity is null)
            return ApiResult<TransferDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

        if (entity.Status is JobStatus.Cancelled or JobStatus.Merged or JobStatus.Completed
            or JobStatus.PendingInvoice or JobStatus.Invoiced)
            return ApiResult<TransferDetailDto>.Fail(
                $"İş bu durumda güncellenemez: {EnumLabelHelper.GetLabel(entity.Status)}", statusCode: 400);

        if (!entity.DriverId.HasValue)
            return ApiResult<TransferDetailDto>.Fail("İşe atanmış sürücü yok.", statusCode: 400);

        var userExists = await UserExistsAsync(userId, ct);
        var now = DateTime.UtcNow;

        // Idempotency
        if (isDepart && entity.DepartedAt.HasValue)
            return await GetTransferDetailByIdAsync(entity.Id, ct);
        if (!isDepart && entity.ContactedAt.HasValue)
            return await GetTransferDetailByIdAsync(entity.Id, ct);

        // Always set Contacted (monotonic auto-fill)
        if (!entity.ContactedAt.HasValue)
            entity.ContactedAt = now;

        if (isDepart)
            entity.DepartedAt = now;

        // First action that takes job operational → JobStatus.InProgress
        if (isDepart && entity.Status != JobStatus.InProgress)
        {
            var oldStatus = entity.Status;
            entity.Status = JobStatus.InProgress;
            if (userExists)
            {
                _db.JobStatusHistories.Add(new Domain.Entities.JobStatusHistory
                {
                    JobId = entity.Id,
                    OldStatus = oldStatus,
                    NewStatus = JobStatus.InProgress,
                    ChangedByUserId = userId,
                    ChangeReason = "Sürücü yola çıktı.",
                    ChangedAt = now
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        await _broadcaster.BroadcastJobsChangedAsync(ct);
        return await GetTransferDetailByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<TransferDetailDto>> MarkStopPickedUpAsync(Guid jobPublicId, long stopId, Guid userId, CancellationToken ct)
        => await UpdateStopProgressAsync(jobPublicId, stopId, userId, isPickup: true, ct);

    public async Task<ApiResult<TransferDetailDto>> MarkStopDroppedOffAsync(Guid jobPublicId, long stopId, Guid userId, CancellationToken ct)
        => await UpdateStopProgressAsync(jobPublicId, stopId, userId, isPickup: false, ct);

    private async Task<ApiResult<TransferDetailDto>> UpdateStopProgressAsync(Guid jobPublicId, long stopId, Guid userId, bool isPickup, CancellationToken ct)
    {
        var entity = await _db.Jobs
            .Include(j => j.Stops)
            .FirstOrDefaultAsync(j => j.PublicId == jobPublicId, ct);
        if (entity is null)
            return ApiResult<TransferDetailDto>.Fail("İş bulunamadı.", statusCode: 404);

        if (entity.Status is JobStatus.Cancelled or JobStatus.Merged or JobStatus.Completed
            or JobStatus.PendingInvoice or JobStatus.Invoiced)
            return ApiResult<TransferDetailDto>.Fail(
                $"İş bu durumda güncellenemez: {EnumLabelHelper.GetLabel(entity.Status)}", statusCode: 400);

        if (!entity.DriverId.HasValue)
            return ApiResult<TransferDetailDto>.Fail("İşe atanmış sürücü yok.", statusCode: 400);

        var userExists = await UserExistsAsync(userId, ct);

        var stop = entity.Stops.FirstOrDefault(s => s.Id == stopId);
        if (stop is null)
            return ApiResult<TransferDetailDto>.Fail("Durak bu işe ait değil.", statusCode: 404);

        var now = DateTime.UtcNow;

        if (isPickup)
        {
            if (stop.PickedUpAt.HasValue)
                return await GetTransferDetailByIdAsync(entity.Id, ct);
            stop.PickedUpAt = now;
        }
        else
        {
            if (!stop.PickedUpAt.HasValue)
                return ApiResult<TransferDetailDto>.Fail("Yolcu önce alınmadan bırakılamaz.", statusCode: 400);
            if (stop.DroppedOffAt.HasValue)
                return await GetTransferDetailByIdAsync(entity.Id, ct);
            stop.DroppedOffAt = now;
        }

        // Auto-set Contacted + Departed (monotonic) on first stop action
        if (!entity.ContactedAt.HasValue)
            entity.ContactedAt = now;
        if (!entity.DepartedAt.HasValue)
            entity.DepartedAt = now;

        if (entity.Status != JobStatus.InProgress
            && entity.Status != JobStatus.Completed)
        {
            var oldStatus = entity.Status;
            entity.Status = JobStatus.InProgress;
            if (userExists)
            {
                _db.JobStatusHistories.Add(new Domain.Entities.JobStatusHistory
                {
                    JobId = entity.Id,
                    OldStatus = oldStatus,
                    NewStatus = JobStatus.InProgress,
                    ChangedByUserId = userId,
                    ChangeReason = isPickup ? "Yolcu alındı." : "Yolcu bırakıldı.",
                    ChangedAt = now
                });
            }
        }

        // All stops dropped off → Completed
        if (!isPickup && entity.Stops.All(s => s.DroppedOffAt.HasValue))
        {
            var oldStatus = entity.Status;
            entity.Status = JobStatus.Completed;
            if (userExists)
            {
                _db.JobStatusHistories.Add(new Domain.Entities.JobStatusHistory
                {
                    JobId = entity.Id,
                    OldStatus = oldStatus,
                    NewStatus = JobStatus.Completed,
                    ChangedByUserId = userId,
                    ChangeReason = "Tüm duraklar tamamlandı.",
                    ChangedAt = now
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        await _broadcaster.BroadcastJobsChangedAsync(ct);
        return await GetTransferDetailByIdAsync(entity.Id, ct);
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
            .Include(j => j.Contractor)
            .Include(j => j.VehicleOwner)
            .Include(j => j.Vehicle)
            .Include(j => j.Driver)
            .Include(j => j.CreatedByUser)
            .Include(j => j.AssignedByUser)
            .Include(j => j.MergedIntoJob)
            .Include(j => j.MergedJobs)
            .Include(j => j.Stops).ThenInclude(s => s.Customer)
            .Include(j => j.Stops).ThenInclude(s => s.Passengers).ThenInclude(p => p.Passenger)
            .Include(j => j.Stops).ThenInclude(s => s.PickupLocation)
            .Include(j => j.Stops).ThenInclude(s => s.DropoffLocation)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
    }

    private async Task<string?> ValidateUserAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return "Geçerli bir kullanıcı kimliği (userId) gönderilmelidir.";

        if (!await _db.Users.AnyAsync(u => u.Id == userId, ct))
            return $"Kullanıcı bulunamadı: {userId}";

        return null;
    }

    private async Task<string?> ValidateJobReferencesAsync(long? contractorId, long? vehicleOwnerId, long? vehicleId, long? driverId, CancellationToken ct)
    {
        if (contractorId.HasValue &&
            !await _db.Contractors.AnyAsync(c => c.Id == contractorId.Value, ct))
            return $"Yüklenici bulunamadı: {contractorId.Value}";

        if (vehicleOwnerId.HasValue &&
            !await _db.VehicleOwners.AnyAsync(v => v.Id == vehicleOwnerId.Value, ct))
            return $"Araç sahibi bulunamadı: {vehicleOwnerId.Value}";

        if (vehicleId.HasValue &&
            !await _db.Vehicles.AnyAsync(v => v.Id == vehicleId.Value, ct))
            return $"Araç bulunamadı: {vehicleId.Value}";

        if (driverId.HasValue &&
            !await _db.Drivers.AnyAsync(d => d.Id == driverId.Value, ct))
            return $"Sürücü bulunamadı: {driverId.Value}";

        return null;
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
            .SelectMany(s => s.PassengerIds ?? [])
            .Distinct()
            .ToList();
        if (passengerIds.Count > 0)
        {
            var existing = await _db.Passengers
                .Where(p => passengerIds.Contains(p.Id))
                .Select(p => new { p.Id, p.IsVip })
                .ToListAsync(ct);
            var missingPassengers = passengerIds.Except(existing.Select(p => p.Id)).ToList();
            if (missingPassengers.Count > 0)
                return $"Yolcu bulunamadı: {string.Join(", ", missingPassengers)}";

            // VIP kuralı: VIP yolcu içeren iş tek durak ve tek yolcudan oluşmalıdır.
            // (Başka yolcu veya durak eklenemez; VIP yolcu işte tek olur.)
            var hasVip = existing.Any(p => p.IsVip);
            var totalPassengerSlots = stops.Sum(s => (s.PassengerIds ?? []).Distinct().Count());
            if (hasVip && (stops.Count > 1 || totalPassengerSlots > 1))
                return "VIP yolcu içeren iş yalnızca tek durak ve tek yolcudan oluşabilir. " +
                       "VIP yolcuya başka yolcu veya durak eklenemez.";
        }

        return null;
    }

    /// <summary>
    /// Sadece adres verilen (lokasyon ID'si verilmeyen) duraklar için Locations tablosuna
    /// kayıt açar veya aynı adrese sahip mevcut bir lokasyonu yeniden kullanır,
    /// ardından ilgili stop request'i çözümlenmiş ID'lerle döner.
    /// Adresten üretilen lokasyonlar, stop'a bağlı yolcu varsa otomatik olarak
    /// PassengerLocations üzerinden o yolcuya bağlanır (idempotent).
    /// </summary>
    private async Task<List<JobStopRequest>> ResolveStopLocationsAsync(List<JobStopRequest> stops, CancellationToken ct)
    {
        var result = new List<JobStopRequest>(stops.Count);
        var addressCache = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var passengerLinks = new HashSet<(long PassengerId, long LocationId)>();

        foreach (var s in stops)
        {
            var pickupId = s.PickupLocationId;
            var dropoffId = s.DropoffLocationId;
            var pickupAddress = s.PickupAddress?.Trim();
            var dropoffAddress = s.DropoffAddress?.Trim();
            var pickupFromAddress = false;
            var dropoffFromAddress = false;

            if (!pickupId.HasValue && !string.IsNullOrWhiteSpace(pickupAddress))
            {
                pickupId = await GetOrCreateLocationByAddressAsync(pickupAddress, addressCache, ct);
                pickupFromAddress = true;
            }

            if (!dropoffId.HasValue && !string.IsNullOrWhiteSpace(dropoffAddress))
            {
                dropoffId = await GetOrCreateLocationByAddressAsync(dropoffAddress, addressCache, ct);
                dropoffFromAddress = true;
            }

            foreach (var passengerId in (s.PassengerIds ?? []).Distinct())
            {
                if (pickupFromAddress && pickupId.HasValue)
                    passengerLinks.Add((passengerId, pickupId.Value));
                if (dropoffFromAddress && dropoffId.HasValue)
                    passengerLinks.Add((passengerId, dropoffId.Value));
            }

            result.Add(s with
            {
                PickupLocationId = pickupId,
                DropoffLocationId = dropoffId
            });
        }

        await LinkLocationsToPassengersAsync(passengerLinks, ct);

        return result;
    }

    private async Task LinkLocationsToPassengersAsync(HashSet<(long PassengerId, long LocationId)> links, CancellationToken ct)
    {
        if (links.Count == 0) return;

        var passengerIds = links.Select(l => l.PassengerId).Distinct().ToList();
        var existing = await _db.PassengerLocations
            .Where(pl => passengerIds.Contains(pl.PassengerId))
            .Select(pl => new { pl.PassengerId, pl.LocationId })
            .ToListAsync(ct);

        var existingSet = existing.Select(e => (e.PassengerId, e.LocationId)).ToHashSet();

        foreach (var link in links)
        {
            if (existingSet.Contains(link)) continue;
            _db.PassengerLocations.Add(new PassengerLocation
            {
                PassengerId = link.PassengerId,
                LocationId = link.LocationId
            });
            existingSet.Add(link);
        }
    }

    private async Task<long> GetOrCreateLocationByAddressAsync(string address, Dictionary<string, long> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(address, out var cachedId))
            return cachedId;

        var existing = await _db.Locations
            .Where(l => l.Address != null && l.Address.ToLower() == address.ToLower())
            .Select(l => (long?)l.Id)
            .FirstOrDefaultAsync(ct);

        if (existing.HasValue)
        {
            cache[address] = existing.Value;
            return existing.Value;
        }

        var name = address.Length > 200 ? address[..200] : address;
        var location = new Domain.Entities.Location
        {
            Name = name,
            Address = address,
            LocationType = LocationType.Other,
            IsActive = true
        };

        _db.Locations.Add(location);
        await _db.SaveChangesAsync(ct);

        cache[address] = location.Id;
        return location.Id;
    }

    private static JobStop BuildStop(JobStopRequest s, int sequence)
    {
        var stop = new JobStop
        {
            Sequence = sequence,
            CustomerId = s.CustomerId,
            PickupLocationId = s.PickupLocationId,
            DropoffLocationId = s.DropoffLocationId,
            PickupAddress = s.PickupAddress?.Trim(),
            DropoffAddress = s.DropoffAddress?.Trim(),
            FlightCode = s.FlightCode?.Trim(),
            Notes = s.Notes?.Trim()
        };

        foreach (var passengerId in (s.PassengerIds ?? []).Distinct())
            stop.Passengers.Add(new JobStopPassenger { PassengerId = passengerId });

        return stop;
    }

    /// <summary>
    /// JobStatus geçişinin DriverStage ile tutarlı olup olmadığını doğrular.
    /// "Tamamlandı / Fatura Kesilecek / Faturalandı" durumlarına geçmek için
    /// tüm durakların bırakılmış olması (DriverStage == DroppedOff) gerekir.
    /// "Devam Ediyor" durumuna geçmek için işin bir sürücüsü olmalıdır.
    /// </summary>
    private static string? ValidateStatusAgainstDriverStage(Domain.Entities.Job entity, JobStatus newStatus)
    {
        var stage = DriverStageHelper.Resolve(entity);
        var allDroppedOff = entity.Stops.Count > 0 && entity.Stops.All(s => s.DroppedOffAt.HasValue);

        switch (newStatus)
        {
            case JobStatus.Completed:
            case JobStatus.PendingInvoice:
            case JobStatus.Invoiced:
                if (!allDroppedOff)
                    return $"İş '{EnumLabelHelper.GetLabel(newStatus)}' durumuna alınamaz: " +
                           $"sürücü aşaması '{EnumLabelHelper.GetLabel(stage)}'. " +
                           $"Tüm duraklar bırakılmadan iş ilerletilemez, durum 'Devam Ediyor' olarak kalmalı.";
                break;

            case JobStatus.InProgress:
                if (!entity.DriverId.HasValue)
                    return "İş 'Devam Ediyor' durumuna alınamaz: atanmış sürücü yok.";
                break;
        }

        return null;
    }

    private async Task<string> GenerateJobNumberAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var prefix = $"ERT-{today:yyyyMMdd}-";

        // Soft-delete edilmiş işler de IX_Jobs_JobNumber unique index'inde yer kapladığı için
        // sorgu filtresini yok say; ayrıca sayım yerine mevcut en yüksek sıra numarasını baz al
        // ki silme sonrası numara çakışması (23505) yaşanmasın.
        var todaysNumbers = await _db.Jobs
            .IgnoreQueryFilters()
            .Where(j => j.JobNumber.StartsWith(prefix))
            .Select(j => j.JobNumber)
            .ToListAsync(ct);

        var maxSeq = todaysNumbers
            .Select(n => int.TryParse(n[prefix.Length..], out var seq) ? seq : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{(maxSeq + 1):D4}";
    }
}
