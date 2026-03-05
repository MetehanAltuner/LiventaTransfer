using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.User;
using LiventaTransfer.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class UserService
{
    private readonly IAppDbContext _db;
    public UserService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PagedResult<UserListDto>>> GetPagedAsync(PagedQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Users.AsNoTracking()
            .Include(u => u.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(u => u.Username.ToLower().Contains(query.Search.ToLower())
                          || u.FirstName.ToLower().Contains(query.Search.ToLower())
                          || u.LastName.ToLower().Contains(query.Search.ToLower()));

        if (query.IsActive.HasValue)
            q = q.Where(u => u.IsActive == query.IsActive.Value);

        var total = await q.LongCountAsync(ct);

        q = (query.SortBy?.ToLower()) switch
        {
            "username" => query.SortDesc ? q.OrderByDescending(u => u.Username) : q.OrderBy(u => u.Username),
            "name" => query.SortDesc ? q.OrderByDescending(u => u.FirstName) : q.OrderBy(u => u.FirstName),
            _ => q.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => UserListDto.FromEntity(u))
            .ToListAsync(ct);

        return ApiResult<PagedResult<UserListDto>>.Ok(new PagedResult<UserListDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        }, "Kullanıcılar listelendi.");
    }

    public async Task<ApiResult<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Users.AsNoTracking()
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (entity is null)
            return ApiResult<UserDetailDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        return ApiResult<UserDetailDto>.Ok(UserDetailDto.FromEntity(entity), "Kullanıcı bulundu.");
    }

    public async Task<ApiResult<UserDetailDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null)
            return ApiResult<UserDetailDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        if (!await _db.Branches.AnyAsync(b => b.Id == request.BranchId, ct))
            return ApiResult<UserDetailDto>.Fail("Şube bulunamadı.", statusCode: 400);

        entity.FirstName = request.FirstName.Trim();
        entity.LastName = request.LastName.Trim();
        entity.Role = request.Role;
        entity.BranchId = request.BranchId;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<ApiResult<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null)
            return ApiResult<bool>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        entity.IsDeleted = true;
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Kullanıcı silindi.");
    }
}
