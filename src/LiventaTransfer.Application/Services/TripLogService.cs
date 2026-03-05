using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.TripLog;
using LiventaTransfer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class TripLogService
{
    private readonly IAppDbContext _db;
    public TripLogService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<TripLogDto>> GetByJobIdAsync(Guid jobId, CancellationToken ct)
    {
        if (!await _db.Jobs.AnyAsync(j => j.Id == jobId, ct))
            return ApiResult<TripLogDto>.Fail("İş bulunamadı.", statusCode: 404);

        var entity = await _db.TripLogs
            .AsNoTracking()
            .Include(t => t.Driver)
            .FirstOrDefaultAsync(t => t.JobId == jobId, ct);

        if (entity is null)
            return ApiResult<TripLogDto>.Fail("Sefer kaydı bulunamadı.", statusCode: 404);

        return ApiResult<TripLogDto>.Ok(TripLogDto.FromEntity(entity), "Sefer kaydı bulundu.");
    }

    public async Task<ApiResult<TripLogDto>> CreateAsync(Guid jobId, CreateTripLogRequest request, CancellationToken ct)
    {
        if (!await _db.Jobs.AnyAsync(j => j.Id == jobId, ct))
            return ApiResult<TripLogDto>.Fail("İş bulunamadı.", statusCode: 404);

        if (!await _db.Drivers.AnyAsync(d => d.Id == request.DriverId, ct))
            return ApiResult<TripLogDto>.Fail("Şoför bulunamadı.", statusCode: 400);

        if (await _db.TripLogs.AnyAsync(t => t.JobId == jobId, ct))
            return ApiResult<TripLogDto>.Fail("Bu iş için zaten bir sefer kaydı mevcut.", statusCode: 409);

        var entity = new Domain.Entities.TripLog
        {
            JobId = jobId,
            DriverId = request.DriverId,
            PickupTime = request.PickupTime,
            DropoffTime = request.DropoffTime,
            StartKm = request.StartKm,
            EndKm = request.EndKm,
            WaitingMinutes = request.WaitingMinutes,
            FlightStatus = request.FlightStatus?.Trim(),
            DriverNotes = request.DriverNotes?.Trim()
        };

        _db.TripLogs.Add(entity);
        await _db.SaveChangesAsync(ct);

        var created = await _db.TripLogs
            .AsNoTracking()
            .Include(t => t.Driver)
            .FirstAsync(t => t.Id == entity.Id, ct);

        return ApiResult<TripLogDto>.Ok(TripLogDto.FromEntity(created), "Sefer kaydı oluşturuldu.", 201);
    }

    public async Task<ApiResult<TripLogDto>> UpdateAsync(Guid id, UpdateTripLogRequest request, CancellationToken ct)
    {
        var entity = await _db.TripLogs.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null)
            return ApiResult<TripLogDto>.Fail("Sefer kaydı bulunamadı.", statusCode: 404);

        if (!await _db.Drivers.AnyAsync(d => d.Id == request.DriverId, ct))
            return ApiResult<TripLogDto>.Fail("Şoför bulunamadı.", statusCode: 400);

        entity.DriverId = request.DriverId;
        entity.PickupTime = request.PickupTime;
        entity.DropoffTime = request.DropoffTime;
        entity.StartKm = request.StartKm;
        entity.EndKm = request.EndKm;
        entity.WaitingMinutes = request.WaitingMinutes;
        entity.FlightStatus = request.FlightStatus?.Trim();
        entity.DriverNotes = request.DriverNotes?.Trim();

        await _db.SaveChangesAsync(ct);

        var updated = await _db.TripLogs
            .AsNoTracking()
            .Include(t => t.Driver)
            .FirstAsync(t => t.Id == entity.Id, ct);

        return ApiResult<TripLogDto>.Ok(TripLogDto.FromEntity(updated), "Sefer kaydı güncellendi.");
    }
}
