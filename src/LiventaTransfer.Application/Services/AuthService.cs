using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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

        var response = await IssueTokensAsync(user, ct);

        return ApiResult<AuthResponse>.Ok(response, "Giriş başarılı.");
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
            existing.FirstName = NameFormatter.ToTitleCase(request.FirstName);
            existing.LastName = NameFormatter.ToTitleCase(request.LastName);
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
                FirstName = NameFormatter.ToTitleCase(request.FirstName),
                LastName = NameFormatter.ToTitleCase(request.LastName),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                BranchId = request.BranchId,
                IsActive = true
            };
            _db.Users.Add(user);
        }

        await _db.SaveChangesAsync(ct);

        var response = await IssueTokensAsync(user, ct);

        return ApiResult<AuthResponse>.Ok(response, "Kayıt başarılı.", 201);
    }

    public async Task<ApiResult<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var refreshToken = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, ct);

        if (refreshToken is null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
            return ApiResult<AuthResponse>.Fail("Geçersiz veya süresi dolmuş refresh token.", statusCode: 401);

        if (!refreshToken.User.IsActive)
            return ApiResult<AuthResponse>.Fail("Kullanıcı hesabı pasif.", statusCode: 401);

        // Rotation: kullanılan refresh token tek seferliktir, yenisiyle değiştirilir.
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;

        var response = await IssueTokensAsync(refreshToken.User, ct);

        return ApiResult<AuthResponse>.Ok(response, "Token yenilendi.");
    }

    public async Task<ApiResult<bool>> LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
        }

        if (activeTokens.Count > 0)
            await _db.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Başarıyla çıkış yapıldı.");
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
            FirstName = NameFormatter.ToTitleCase(user.FirstName),
            LastName = NameFormatter.ToTitleCase(user.LastName),
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

    /// <summary>
    /// Access token + refresh token çifti üretir. Refresh token JWT değildir;
    /// 32 baytlık rastgele bir Base64 string olarak DB'de saklanır ve her
    /// kullanımda yenisiyle değiştirilir (rotation).
    /// </summary>
    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var permissions = await GetPermissionCodesAsync(user.Role, ct);
        var response = GenerateToken(user, permissions);

        var expiryHours = int.Parse(_config["Jwt:RefreshTokenExpiryHours"] ?? "8");
        var tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(tokenBytes),
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours)
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        return response with
        {
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiration = refreshToken.ExpiresAt
        };
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
