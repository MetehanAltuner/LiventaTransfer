using System.IdentityModel.Tokens.Jwt;
using LiventaTransfer.Application.DTOs.Auth;
using LiventaTransfer.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Kimlik Doğrulama</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Kimlik Doğrulama")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Kullanıcı girişi → token döner</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var r = await _auth.LoginAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Yeni kullanıcı kaydı</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var r = await _auth.RegisterAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Şifre değiştirme (kendi şifresi)</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var r = await _auth.ChangePasswordAsync(userId.Value, request, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Mevcut kullanıcı bilgisi (token'dan)</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var r = await _auth.GetCurrentUserAsync(userId.Value, ct);
        return StatusCode(r.StatusCode, r);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
