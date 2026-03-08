using LiventaTransfer.Application.DTOs.Auth;
using LiventaTransfer.Application.Interfaces.Services;
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
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var r = await _auth.LoginAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Yeni kullanıcı kaydı</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var r = await _auth.RegisterAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Şifre değiştirme</summary>
    [HttpPost("change-password/{userId:guid}")]
    public async Task<IActionResult> ChangePassword(Guid userId, [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var r = await _auth.ChangePasswordAsync(userId, request, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Kullanıcı bilgisi</summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken ct)
    {
        var r = await _auth.GetCurrentUserAsync(userId, ct);
        return StatusCode(r.StatusCode, r);
    }
}
