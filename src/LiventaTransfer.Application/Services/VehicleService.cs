using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Vehicle;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class VehicleService
{
    private readonly IAppDbContext _db;
    public VehicleService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<VehicleListDto>>> GetPagedAsync(PagedQuery query, VehicleType? vehicleType, Guid? vehicleOwnerId, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Vehicles.AsNoTracking().Include(v => v.VehicleOwner).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(v => v.Plate.ToLower().Contains(query.Search.ToLower()) ||
                             (v.Brand != null && v.Brand.ToLower().Contains(query.Search.ToLower())));

        if (query.IsActive.HasValue)
            q = q.Where(v => v.IsActive == query.IsActive.Value);

        if (vehicleType.HasValue)
            q = q.Where(v => v.VehicleType == vehicleType.Value);

        if (vehicleOwnerId.HasValue)
            q = q.Where(v => v.VehicleOwnerId == vehicleOwnerId.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "plate" => query.SortDesc ? q.OrderByDescending(v => v.Plate) : q.OrderBy(v => v.Plate),
            _ => query.SortDesc ? q.OrderByDescending(v => v.CreatedAt) : q.OrderBy(v => v.Plate)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => VehicleListDto.FromEntity(v))
            .ToListAsync(ct);

        return ApiResult<PagedResult<VehicleListDto>>.Ok(new PagedResult<VehicleListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "Araçlar listelendi.");
    }

    public async Task<ApiResult<VehicleDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleOwner)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (entity is null)
            return ApiResult<VehicleDetailDto>.Fail("Araç bulunamadı.", statusCode: 404);

        return ApiResult<VehicleDetailDto>.Ok(VehicleDetailDto.FromEntity(entity), "Araç bulundu.");
    }

    public async Task<ApiResult<VehicleDetailDto>> CreateAsync(CreateVehicleRequest request, CancellationToken ct)
    {
        if (!await _db.VehicleOwners.AnyAsync(v => v.Id == request.VehicleOwnerId, ct))
            return ApiResult<VehicleDetailDto>.Fail("Araç sahibi bulunamadı.", statusCode: 400);

        var plateNorm = request.Plate.Trim().ToUpperInvariant();
        if (await _db.Vehicles.AnyAsync(v => v.Plate.ToUpper() == plateNorm, ct))
            return ApiResult<VehicleDetailDto>.Fail("Bu plaka zaten kayıtlı.", statusCode: 409);

        var entity = new Domain.Entities.Vehicle
        {
            Plate = plateNorm,
            VehicleType = request.VehicleType,
            Brand = request.Brand?.Trim(),
            Model = request.Model?.Trim(),
            Year = request.Year,
            Capacity = request.Capacity,
            VehicleOwnerId = request.VehicleOwnerId,
            IsActive = true
        };

        _db.Vehicles.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ApiResult<VehicleDetailDto>.Ok(VehicleDetailDto.FromEntity(entity), "Araç oluşturuldu.", 201);
    }

    public async Task<ApiResult<VehicleDetailDto>> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken ct)
    {
        var entity = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null)
            return ApiResult<VehicleDetailDto>.Fail("Araç bulunamadı.", statusCode: 404);

        if (!await _db.VehicleOwners.AnyAsync(v => v.Id == request.VehicleOwnerId, ct))
            return ApiResult<VehicleDetailDto>.Fail("Araç sahibi bulunamadı.", statusCode: 400);

        var plateNorm = request.Plate.Trim().ToUpperInvariant();
        if (await _db.Vehicles.AnyAsync(v => v.Plate.ToUpper() == plateNorm && v.Id != id, ct))
            return ApiResult<VehicleDetailDto>.Fail("Bu plaka zaten kayıtlı.", statusCode: 409);

        entity.Plate = plateNorm;
        entity.VehicleType = request.VehicleType;
        entity.Brand = request.Brand?.Trim();
        entity.Model = request.Model?.Trim();
        entity.Year = request.Year;
        entity.Capacity = request.Capacity;
        entity.VehicleOwnerId = request.VehicleOwnerId;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return ApiResult<VehicleDetailDto>.Ok(VehicleDetailDto.FromEntity(entity), "Araç güncellendi.");
    }

    public async Task<ApiResult<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Araç bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Araç silindi.");
    }
}
