using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Branch;
using LiventaTransfer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class BranchService
{
    private readonly IAppDbContext _db;
    public BranchService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<BranchListDto>>> GetPagedAsync(PagedQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Branches.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(b => b.Name.ToLower().Contains(query.Search.ToLower()));

        if (query.IsActive.HasValue)
            q = q.Where(b => b.IsActive == query.IsActive.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "name" => query.SortDesc ? q.OrderByDescending(b => b.Name) : q.OrderBy(b => b.Name),
            _ => query.SortDesc ? q.OrderByDescending(b => b.CreatedAt) : q.OrderBy(b => b.Name)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => BranchListDto.FromEntity(b))
            .ToListAsync(ct);

        var paged = new PagedResult<BranchListDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        return ApiResult<PagedResult<BranchListDto>>.Ok(paged, "Şubeler listelendi.");
    }

    public async Task<ApiResult<BranchDetailDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        if (entity is null)
            return ApiResult<BranchDetailDto>.Fail("Şube bulunamadı.", statusCode: 404);

        return ApiResult<BranchDetailDto>.Ok(BranchDetailDto.FromEntity(entity), "Şube bulundu.");
    }

    public async Task<ApiResult<BranchDetailDto>> CreateAsync(CreateBranchRequest request, CancellationToken ct)
    {
        var entity = new Domain.Entities.Branch
        {
            Name = request.Name.Trim(),
            Address = request.Address?.Trim(),
            IsActive = true
        };

        _db.Branches.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ApiResult<BranchDetailDto>.Ok(BranchDetailDto.FromEntity(entity), "Şube oluşturuldu.", 201);
    }

    public async Task<ApiResult<BranchDetailDto>> UpdateAsync(long id, UpdateBranchRequest request, CancellationToken ct)
    {
        var entity = await _db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (entity is null)
            return ApiResult<BranchDetailDto>.Fail("Şube bulunamadı.", statusCode: 404);

        entity.Name = request.Name.Trim();
        entity.Address = request.Address?.Trim();
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return ApiResult<BranchDetailDto>.Ok(BranchDetailDto.FromEntity(entity), "Şube güncellendi.");
    }

    public async Task<ApiResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Şube bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Şube silindi.");
    }
}
