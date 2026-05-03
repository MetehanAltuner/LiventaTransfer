using LiventaTransfer.Application.DTOs.Permission;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>İzinler ve rol-izin yönetimi (frontend tab kontrolü için).</summary>
[ApiController]
[Route("api")]
[Tags("İzinler")]
public sealed class PermissionsController : ControllerBase
{
    private readonly PermissionService _svc;
    public PermissionsController(PermissionService svc) => _svc = svc;

    /// <summary>Tüm izinler (frontend sidebar lookup'ı).</summary>
    [HttpGet("lookups/permissions")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var r = await _svc.GetAllAsync(ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Bir kullanıcının erişebileceği izinler. Frontend giriş sonrası tab'ları buradan çeker.</summary>
    [HttpGet("users/{userId:guid}/permissions")]
    public async Task<IActionResult> GetForUser(Guid userId, CancellationToken ct)
    {
        var r = await _svc.GetForUserAsync(userId, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Tüm rol-izin matrisi (yönetim ekranı için).</summary>
    [HttpGet("admin/role-permissions")]
    public async Task<IActionResult> GetMatrix(CancellationToken ct)
    {
        var r = await _svc.GetMatrixAsync(ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Bir role atanan izinleri tamamen değiştirir. GeneralManager ve Developer değiştirilemez.</summary>
    [HttpPut("admin/role-permissions/{role}")]
    public async Task<IActionResult> UpdateRolePermissions(UserRole role, [FromBody] UpdateRolePermissionsRequest request, CancellationToken ct)
    {
        var r = await _svc.UpdateRolePermissionsAsync(role, request, ct);
        return StatusCode(r.StatusCode, r);
    }
}
