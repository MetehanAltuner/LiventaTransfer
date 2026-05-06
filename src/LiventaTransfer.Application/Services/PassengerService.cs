using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Passenger;
using LiventaTransfer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class PassengerService
{
    private readonly IAppDbContext _db;
    public PassengerService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<PassengerListDto>>> GetPagedAsync(PagedQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Passengers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p => p.FullName.ToLower().Contains(query.Search.ToLower()));

        if (query.IsActive.HasValue)
            q = q.Where(p => p.IsActive == query.IsActive.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "fullname" or "name" => query.SortDesc ? q.OrderByDescending(p => p.FullName) : q.OrderBy(p => p.FullName),
            _ => query.SortDesc ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.FullName)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => PassengerListDto.FromEntity(p))
            .ToListAsync(ct);

        return ApiResult<PagedResult<PassengerListDto>>.Ok(new PagedResult<PassengerListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "Yolcular listelendi.");
    }

    public async Task<ApiResult<PassengerDetailDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Passengers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (entity is null)
            return ApiResult<PassengerDetailDto>.Fail("Yolcu bulunamadı.", statusCode: 404);

        return ApiResult<PassengerDetailDto>.Ok(PassengerDetailDto.FromEntity(entity), "Yolcu bulundu.");
    }

    public async Task<ApiResult<PassengerDetailDto>> CreateAsync(CreatePassengerRequest request, CancellationToken ct)
    {
        var entity = new Domain.Entities.Passenger
        {
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = true
        };

        _db.Passengers.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ApiResult<PassengerDetailDto>.Ok(PassengerDetailDto.FromEntity(entity), "Yolcu oluşturuldu.", 201);
    }

    public async Task<ApiResult<PassengerDetailDto>> UpdateAsync(long id, UpdatePassengerRequest request, CancellationToken ct)
    {
        var entity = await _db.Passengers.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
            return ApiResult<PassengerDetailDto>.Fail("Yolcu bulunamadı.", statusCode: 404);

        entity.FullName = request.FullName.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Email = request.Email?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return ApiResult<PassengerDetailDto>.Ok(PassengerDetailDto.FromEntity(entity), "Yolcu güncellendi.");
    }

    public async Task<ApiResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Passengers.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Yolcu bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Yolcu silindi.");
    }
}
