using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Permission;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Entities;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Services;

public sealed class PermissionService
{
    private static readonly UserRole[] SuperAdminRoles = [UserRole.GeneralManager, UserRole.Developer];

    private readonly IAppDbContext _db;
    public PermissionService(IAppDbContext db) => _db = db;

    public async Task<ApiResult<List<PermissionDto>>> GetAllAsync(CancellationToken ct)
    {
        var items = await _db.Permissions
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => PermissionDto.FromEntity(p))
            .ToListAsync(ct);

        return ApiResult<List<PermissionDto>>.Ok(items, "İzinler.");
    }

    public async Task<ApiResult<List<PermissionDto>>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return ApiResult<List<PermissionDto>>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        var permsQuery = _db.Permissions.AsNoTracking().Where(p => p.IsActive);

        if (!SuperAdminRoles.Contains(user.Role))
        {
            var allowedIds = _db.RolePermissions
                .Where(rp => rp.Role == user.Role)
                .Select(rp => rp.PermissionId);
            permsQuery = permsQuery.Where(p => allowedIds.Contains(p.Id));
        }

        var items = await permsQuery
            .OrderBy(p => p.SortOrder)
            .Select(p => PermissionDto.FromEntity(p))
            .ToListAsync(ct);

        return ApiResult<List<PermissionDto>>.Ok(items, "Kullanıcı izinleri.");
    }

    public async Task<ApiResult<List<RolePermissionsDto>>> GetMatrixAsync(CancellationToken ct)
    {
        var allPerms = await _db.Permissions.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        var rolePerms = await _db.RolePermissions.AsNoTracking().ToListAsync(ct);

        var roles = Enum.GetValues<UserRole>();
        var matrix = roles.Select(role =>
        {
            // Super-admin: always has every permission
            var perms = SuperAdminRoles.Contains(role)
                ? allPerms
                : allPerms.Where(p => rolePerms.Any(rp => rp.Role == role && rp.PermissionId == p.Id)).ToList();

            return new RolePermissionsDto
            {
                Role = role,
                RoleLabel = EnumLabelHelper.GetLabel(role),
                Permissions = perms.Select(PermissionDto.FromEntity).ToList()
            };
        }).ToList();

        return ApiResult<List<RolePermissionsDto>>.Ok(matrix, "Rol-izin matrisi.");
    }

    public async Task<ApiResult<RolePermissionsDto>> UpdateRolePermissionsAsync(
        UserRole role, UpdateRolePermissionsRequest request, CancellationToken ct)
    {
        if (SuperAdminRoles.Contains(role))
            return ApiResult<RolePermissionsDto>.Fail(
                $"{EnumLabelHelper.GetLabel(role)} rolü tüm izinlere sabit olarak sahiptir, değiştirilemez.",
                statusCode: 400);

        var permissionIds = request.PermissionIds?.Distinct().ToList() ?? [];

        if (permissionIds.Count > 0)
        {
            var existing = await _db.Permissions
                .Where(p => permissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(ct);
            var missing = permissionIds.Except(existing).ToList();
            if (missing.Count > 0)
                return ApiResult<RolePermissionsDto>.Fail(
                    $"İzin bulunamadı: {string.Join(", ", missing)}", statusCode: 400);
        }

        var current = await _db.RolePermissions
            .Where(rp => rp.Role == role)
            .ToListAsync(ct);
        _db.RolePermissions.RemoveRange(current);

        foreach (var pid in permissionIds)
            _db.RolePermissions.Add(new RolePermission { Role = role, PermissionId = pid });

        await _db.SaveChangesAsync(ct);

        var perms = await _db.Permissions.AsNoTracking()
            .Where(p => p.IsActive && permissionIds.Contains(p.Id))
            .OrderBy(p => p.SortOrder)
            .Select(p => PermissionDto.FromEntity(p))
            .ToListAsync(ct);

        return ApiResult<RolePermissionsDto>.Ok(new RolePermissionsDto
        {
            Role = role,
            RoleLabel = EnumLabelHelper.GetLabel(role),
            Permissions = perms
        }, "Rol izinleri güncellendi.");
    }
}
