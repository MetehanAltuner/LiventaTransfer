using LiventaTransfer.API.Extensions;
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
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var r = await _auth.LoginAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Yeni kullanıcı kaydı</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var r = await _auth.RegisterAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Şifre değiştirme — kullanıcı JWT'den alınır</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var r = await _auth.ChangePasswordAsync(User.GetUserId(), request, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Oturum açan kullanıcının bilgisi</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var r = await _auth.GetCurrentUserAsync(User.GetUserId(), ct);
        return StatusCode(r.StatusCode, r);
    }
}
