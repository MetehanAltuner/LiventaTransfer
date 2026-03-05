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

        var token = GenerateToken(user);

        return ApiResult<AuthResponse>.Ok(token, "Giriş başarılı.");
    }

    public async Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Username.ToLower() == username, ct))
            return ApiResult<AuthResponse>.Fail("Bu kullanıcı adı zaten mevcut.", statusCode: 409);

        if (!await _db.Branches.AnyAsync(b => b.Id == request.BranchId, ct))
            return ApiResult<AuthResponse>.Fail("Geçersiz şube.", statusCode: 400);

        var user = new User
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
        await _db.SaveChangesAsync(ct);

        var token = GenerateToken(user);

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
            BranchId = user.BranchId,
            BranchName = user.Branch?.Name ?? string.Empty,
            IsActive = user.IsActive
        };

        return ApiResult<UserInfoDto>.Ok(dto, "Kullanıcı bilgisi.");
    }

    private AuthResponse GenerateToken(User user)
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
