using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Notification;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Bildirimler</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Bildirimler")]
public sealed class NotificationsController : ControllerBase
{
    private readonly NotificationService _svc;
    public NotificationsController(NotificationService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] PagedQuery query,
        [FromQuery] NotificationChannel? channel,
        [FromQuery] bool? isDelivered,
        CancellationToken ct)
    {
        var r = await _svc.GetPagedAsync(query, channel, isDelivered, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPatch("{id:guid}/delivered")]
    public async Task<IActionResult> MarkAsDelivered(Guid id, CancellationToken ct)
    {
        var r = await _svc.MarkAsDeliveredAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }
}
