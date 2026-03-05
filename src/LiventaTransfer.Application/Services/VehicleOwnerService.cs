using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Driver;
using LiventaTransfer.Application.DTOs.Vehicle;
using LiventaTransfer.Application.DTOs.VehicleOwner;
using LiventaTransfer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class VehicleOwnerService
{
    private readonly IAppDbContext _db;
    public VehicleOwnerService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<VehicleOwnerListDto>>> GetPagedAsync(PagedQuery query, bool? isOwnFleet, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.VehicleOwners.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(v => v.Name.ToLower().Contains(query.Search.ToLower()));

        if (query.IsActive.HasValue)
            q = q.Where(v => v.IsActive == query.IsActive.Value);

        if (isOwnFleet.HasValue)
            q = q.Where(v => v.IsOwnFleet == isOwnFleet.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "name" => query.SortDesc ? q.OrderByDescending(v => v.Name) : q.OrderBy(v => v.Name),
            _ => query.SortDesc ? q.OrderByDescending(v => v.CreatedAt) : q.OrderBy(v => v.Name)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => VehicleOwnerListDto.FromEntity(v))
            .ToListAsync(ct);

        return ApiResult<PagedResult<VehicleOwnerListDto>>.Ok(new PagedResult<VehicleOwnerListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "Araç sahipleri listelendi.");
    }

    public async Task<ApiResult<VehicleOwnerDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.VehicleOwners
            .AsNoTracking()
            .Include(v => v.Vehicles)
            .Include(v => v.Drivers)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (entity is null)
            return ApiResult<VehicleOwnerDetailDto>.Fail("Araç sahibi bulunamadı.", statusCode: 404);

        return ApiResult<VehicleOwnerDetailDto>.Ok(VehicleOwnerDetailDto.FromEntity(entity), "Araç sahibi bulundu.");
    }

    public async Task<ApiResult<VehicleOwnerDetailDto>> CreateAsync(CreateVehicleOwnerRequest request, CancellationToken ct)
    {
        var entity = new Domain.Entities.VehicleOwner
        {
            Name = request.Name.Trim(),
            IsOwnFleet = request.IsOwnFleet,
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = true
        };

        _db.VehicleOwners.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ApiResult<VehicleOwnerDetailDto>.Ok(VehicleOwnerDetailDto.FromEntity(entity), "Araç sahibi oluşturuldu.", 201);
    }

    public async Task<ApiResult<VehicleOwnerDetailDto>> UpdateAsync(Guid id, UpdateVehicleOwnerRequest request, CancellationToken ct)
    {
        var entity = await _db.VehicleOwners.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null)
            return ApiResult<VehicleOwnerDetailDto>.Fail("Araç sahibi bulunamadı.", statusCode: 404);

        entity.Name = request.Name.Trim();
        entity.IsOwnFleet = request.IsOwnFleet;
        entity.ContactPerson = request.ContactPerson?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Email = request.Email?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return ApiResult<VehicleOwnerDetailDto>.Ok(VehicleOwnerDetailDto.FromEntity(entity), "Araç sahibi güncellendi.");
    }

    public async Task<ApiResult<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.VehicleOwners.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Araç sahibi bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Araç sahibi silindi.");
    }

    public async Task<ApiResult<List<VehicleListDto>>> GetVehiclesAsync(Guid ownerId, CancellationToken ct)
    {
        if (!await _db.VehicleOwners.AnyAsync(v => v.Id == ownerId, ct))
            return ApiResult<List<VehicleListDto>>.Fail("Araç sahibi bulunamadı.", statusCode: 404);

        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleOwner)
            .Where(v => v.VehicleOwnerId == ownerId)
            .Select(v => VehicleListDto.FromEntity(v))
            .ToListAsync(ct);

        return ApiResult<List<VehicleListDto>>.Ok(vehicles, "Araçlar listelendi.");
    }

    public async Task<ApiResult<List<DriverListDto>>> GetDriversAsync(Guid ownerId, CancellationToken ct)
    {
        if (!await _db.VehicleOwners.AnyAsync(v => v.Id == ownerId, ct))
            return ApiResult<List<DriverListDto>>.Fail("Araç sahibi bulunamadı.", statusCode: 404);

        var drivers = await _db.Drivers
            .AsNoTracking()
            .Include(d => d.VehicleOwner)
            .Where(d => d.VehicleOwnerId == ownerId)
            .Select(d => DriverListDto.FromEntity(d))
            .ToListAsync(ct);

        return ApiResult<List<DriverListDto>>.Ok(drivers, "Şoförler listelendi.");
    }
}
