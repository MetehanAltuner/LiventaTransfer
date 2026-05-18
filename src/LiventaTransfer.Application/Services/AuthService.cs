using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Auth;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Application.Interfaces.Services;
using LiventaTransfer.Domain.Entities;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LiventaTransfer.Application.Services;

public sealed class AuthService : IAuthService
{
    private static readonly UserRole[] SuperAdminRoles = [UserRole.GeneralManager, UserRole.Developer];

    private readonly IAppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(IAppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username, ct);

        if (user is null)
            return ApiResult<AuthResponse>.Fail("Geçersiz kullanıcı adı veya şifre.", statusCode: 401);

        if (!user.IsActive)
            return ApiResult<AuthResponse>.Fail("Kullanıcı hesabı pasif.", statusCode: 401);

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResult<AuthResponse>.Fail("Geçersiz kullanıcı adı veya şifre.", statusCode: 401);

        var permissions = await GetPermissionCodesAsync(user.Role, ct);
        var token = GenerateToken(user, permissions);

        return ApiResult<AuthResponse>.Ok(token, "Giriş başarılı.");
    }

    public async Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        if (!await _db.Branches.AnyAsync(b => b.Id == request.BranchId, ct))
            return ApiResult<AuthResponse>.Fail("Geçersiz şube.", statusCode: 400);

        // Soft-deleted kayıtları da görmek için query filter'ı bypass ediyoruz.
        // Aynı kullanıcı adında soft-deleted bir kayıt varsa onu reaktive edip güncelliyoruz;
        // unique index soft-delete'leri de kapsadığı için yeni satır INSERT etmek 409 üretirdi.
        var existing = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username, ct);

        User user;
        if (existing is not null)
        {
            if (!existing.IsDeleted)
                return ApiResult<AuthResponse>.Fail("Bu kullanıcı adı zaten mevcut.", statusCode: 409);

            existing.IsDeleted = false;
            existing.IsActive = true;
            existing.FirstName = request.FirstName.Trim();
            existing.LastName = request.LastName.Trim();
            existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            existing.Role = request.Role;
            existing.BranchId = request.BranchId;
            user = existing;
        }
        else
        {
            user = new User
            {
                Username = username,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                BranchId = request.BranchId,
                IsActive = true
            };
            _db.Users.Add(user);
        }

        await _db.SaveChangesAsync(ct);

        var permissions = await GetPermissionCodesAsync(user.Role, ct);
        var token = GenerateToken(user, permissions);

        return ApiResult<AuthResponse>.Ok(token, "Kayıt başarılı.", 201);
    }

    public async Task<ApiResult<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return ApiResult<bool>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return ApiResult<bool>.Fail("Mevcut şifre hatalı.", statusCode: 400);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Şifre başarıyla değiştirildi.");
    }

    public async Task<ApiResult<UserInfoDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return ApiResult<UserInfoDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

        var dto = new UserInfoDto
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            RoleLabel = Common.EnumLabelHelper.GetLabel(user.Role),
            BranchId = user.BranchId,
            BranchName = user.Branch?.Name ?? string.Empty,
            IsActive = user.IsActive
        };

        return ApiResult<UserInfoDto>.Ok(dto, "Kullanıcı bilgisi.");
    }

    /// <summary>
    /// Returns the permission codes available to the given role.
    /// GeneralManager and Developer are super-admins and receive every active permission;
    /// other roles get only what's mapped in RolePermissions.
    /// </summary>
    private async Task<List<string>> GetPermissionCodesAsync(UserRole role, CancellationToken ct)
    {
        if (SuperAdminRoles.Contains(role))
        {
            return await _db.Permissions.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => p.Code)
                .ToListAsync(ct);
        }

        return await _db.RolePermissions.AsNoTracking()
            .Where(rp => rp.Role == role)
            .Join(_db.Permissions.Where(p => p.IsActive),
                  rp => rp.PermissionId,
                  p => p.Id,
                  (rp, p) => new { p.Code, p.SortOrder })
            .OrderBy(x => x.SortOrder)
            .Select(x => x.Code)
            .ToListAsync(ct);
    }

    private AuthResponse GenerateToken(User user, IReadOnlyCollection<string> permissionCodes)
    {
        var key = _config["Jwt:Key"]!;
        var issuer = _config["Jwt:Issuer"]!;
        var audience = _config["Jwt:Audience"]!;
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("given_name", user.FirstName),
            new("surname", user.LastName),
            new("role", user.Role.ToString()),
            new("branch_id", user.BranchId.ToString()),
            new("is_active", user.IsActive ? "true" : "false")
        };

        // Each permission added as its own "permission" claim so it round-trips
        // through ClaimsPrincipal as multiple values (User.FindAll("permission")).
        foreach (var code in permissionCodes)
            claims.Add(new Claim("permission", code));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials
        );

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expiration = expires,
            Username = user.Username
        };
    }
}
