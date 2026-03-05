using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Location;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class LocationService
{
    private readonly IAppDbContext _db;
    public LocationService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<LocationListDto>>> GetPagedAsync(PagedQuery query, LocationType? locationType, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Locations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(l => l.Name.ToLower().Contains(query.Search.ToLower()) ||
                             (l.ShortCode != null && l.ShortCode.ToLower().Contains(query.Search.ToLower())));

        if (query.IsActive.HasValue)
            q = q.Where(l => l.IsActive == query.IsActive.Value);

        if (locationType.HasValue)
            q = q.Where(l => l.LocationType == locationType.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "name" => query.SortDesc ? q.OrderByDescending(l => l.Name) : q.OrderBy(l => l.Name),
            _ => query.SortDesc ? q.OrderByDescending(l => l.CreatedAt) : q.OrderBy(l => l.Name)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => LocationListDto.FromEntity(l))
            .ToListAsync(ct);

        return ApiResult<PagedResult<LocationListDto>>.Ok(new PagedResult<LocationListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "Lokasyonlar listelendi.");
    }

    public async Task<ApiResult<LocationDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (entity is null)
            return ApiResult<LocationDetailDto>.Fail("Lokasyon bulunamadı.", statusCode: 404);

        return ApiResult<LocationDetailDto>.Ok(LocationDetailDto.FromEntity(entity), "Lokasyon bulundu.");
    }

    public async Task<ApiResult<LocationDetailDto>> CreateAsync(CreateLocationRequest request, CancellationToken ct)
    {
        var entity = new Domain.Entities.Location
        {
            Name = request.Name.Trim(),
            ShortCode = request.ShortCode?.Trim(),
            Address = request.Address?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationType = request.LocationType,
            IsActive = true
        };

        _db.Locations.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ApiResult<LocationDetailDto>.Ok(LocationDetailDto.FromEntity(entity), "Lokasyon oluşturuldu.", 201);
    }

    public async Task<ApiResult<LocationDetailDto>> UpdateAsync(Guid id, UpdateLocationRequest request, CancellationToken ct)
    {
        var entity = await _db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (entity is null)
            return ApiResult<LocationDetailDto>.Fail("Lokasyon bulunamadı.", statusCode: 404);

        entity.Name = request.Name.Trim();
        entity.ShortCode = request.ShortCode?.Trim();
        entity.Address = request.Address?.Trim();
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.LocationType = request.LocationType;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return ApiResult<LocationDetailDto>.Ok(LocationDetailDto.FromEntity(entity), "Lokasyon güncellendi.");
    }

    public async Task<ApiResult<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Lokasyon bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Lokasyon silindi.");
    }
}
