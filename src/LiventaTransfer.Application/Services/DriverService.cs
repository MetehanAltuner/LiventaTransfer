using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Driver;
using LiventaTransfer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class DriverService
{
    private readonly IAppDbContext _db;
    public DriverService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<DriverListDto>>> GetPagedAsync(PagedQuery query, long? vehicleOwnerId, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Drivers.AsNoTracking().Include(d => d.VehicleOwner).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(d => d.FullName.ToLower().Contains(query.Search.ToLower()));

        if (query.IsActive.HasValue)
            q = q.Where(d => d.IsActive == query.IsActive.Value);

        if (vehicleOwnerId.HasValue)
            q = q.Where(d => d.VehicleOwnerId == vehicleOwnerId.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "fullname" or "name" => query.SortDesc ? q.OrderByDescending(d => d.FullName) : q.OrderBy(d => d.FullName),
            _ => query.SortDesc ? q.OrderByDescending(d => d.CreatedAt) : q.OrderBy(d => d.FullName)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => DriverListDto.FromEntity(d))
            .ToListAsync(ct);

        return ApiResult<PagedResult<DriverListDto>>.Ok(new PagedResult<DriverListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "Şoförler listelendi.");
    }

    public async Task<ApiResult<DriverDetailDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Drivers
            .AsNoTracking()
            .Include(d => d.VehicleOwner)
            .Include(d => d.DefaultVehicle)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (entity is null)
            return ApiResult<DriverDetailDto>.Fail("Şoför bulunamadı.", statusCode: 404);

        return ApiResult<DriverDetailDto>.Ok(DriverDetailDto.FromEntity(entity), "Şoför bulundu.");
    }

    public async Task<ApiResult<DriverDetailDto>> CreateAsync(CreateDriverRequest request, CancellationToken ct)
    {
        if (!await _db.VehicleOwners.AnyAsync(v => v.Id == request.VehicleOwnerId, ct))
            return ApiResult<DriverDetailDto>.Fail("Araç sahibi bulunamadı.", statusCode: 400);

        if (request.DefaultVehicleId.HasValue &&
            !await _db.Vehicles.AnyAsync(v => v.Id == request.DefaultVehicleId.Value, ct))
            return ApiResult<DriverDetailDto>.Fail("Varsayılan araç bulunamadı.", statusCode: 400);

        var entity = new Domain.Entities.Driver
        {
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            WhatsAppPhone = request.WhatsAppPhone?.Trim(),
            LicenseNumber = request.LicenseNumber?.Trim(),
            VehicleOwnerId = request.VehicleOwnerId,
            DefaultVehicleId = request.DefaultVehicleId,
            IsActive = true
        };

        _db.Drivers.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ApiResult<DriverDetailDto>.Ok(DriverDetailDto.FromEntity(entity), "Şoför oluşturuldu.", 201);
    }

    public async Task<ApiResult<DriverDetailDto>> UpdateAsync(long id, UpdateDriverRequest request, CancellationToken ct)
    {
        var entity = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null)
            return ApiResult<DriverDetailDto>.Fail("Şoför bulunamadı.", statusCode: 404);

        if (!await _db.VehicleOwners.AnyAsync(v => v.Id == request.VehicleOwnerId, ct))
            return ApiResult<DriverDetailDto>.Fail("Araç sahibi bulunamadı.", statusCode: 400);

        if (request.DefaultVehicleId.HasValue &&
            !await _db.Vehicles.AnyAsync(v => v.Id == request.DefaultVehicleId.Value, ct))
            return ApiResult<DriverDetailDto>.Fail("Varsayılan araç bulunamadı.", statusCode: 400);

        entity.FullName = request.FullName.Trim();
        entity.Phone = request.Phone.Trim();
        entity.WhatsAppPhone = request.WhatsAppPhone?.Trim();
        entity.LicenseNumber = request.LicenseNumber?.Trim();
        entity.VehicleOwnerId = request.VehicleOwnerId;
        entity.DefaultVehicleId = request.DefaultVehicleId;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return ApiResult<DriverDetailDto>.Ok(DriverDetailDto.FromEntity(entity), "Şoför güncellendi.");
    }

    public async Task<ApiResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Şoför bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Şoför silindi.");
    }
}
