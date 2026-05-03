using LiventaTransfer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Sürücü Mobil Akışı</summary>
[ApiController]
[Route("api/driver")]
[Tags("Sürücü")]
public sealed class DriverController : ControllerBase
{
    private readonly JobService _svc;
    public DriverController(JobService svc) => _svc = svc;

    /// <summary>Transfer Detayı sayfasını besler.</summary>
    [HttpGet("jobs/{publicId:guid}")]
    public async Task<IActionResult> GetTransferDetail(Guid publicId, CancellationToken ct)
    {
        var r = await _svc.GetTransferDetailAsync(publicId, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Yolcu ile İletişime Geçtim — yolcuyla iletişim kurulduğunu işaretler.</summary>
    [HttpPost("jobs/{publicId:guid}/contact")]
    public async Task<IActionResult> Contact(Guid publicId, [FromQuery] Guid userId, CancellationToken ct)
    {
        var r = await _svc.MarkContactedAsync(publicId, userId, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Yola Çıktım — sürücünün yola çıktığını işaretler.</summary>
    [HttpPost("jobs/{publicId:guid}/depart")]
    public async Task<IActionResult> Depart(Guid publicId, [FromQuery] Guid userId, CancellationToken ct)
    {
        var r = await _svc.MarkDepartedAsync(publicId, userId, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Aldım — ilgili durakta yolcuyu alındı olarak işaretler.</summary>
    [HttpPost("jobs/{publicId:guid}/stops/{stopId:long}/pickup")]
    public async Task<IActionResult> Pickup(Guid publicId, long stopId, [FromQuery] Guid userId, CancellationToken ct)
    {
        var r = await _svc.MarkStopPickedUpAsync(publicId, stopId, userId, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Bıraktım — ilgili durakta yolcuyu bırakıldı olarak işaretler. Tüm duraklar bırakılınca iş Tamamlandı olur.</summary>
    [HttpPost("jobs/{publicId:guid}/stops/{stopId:long}/dropoff")]
    public async Task<IActionResult> Dropoff(Guid publicId, long stopId, [FromQuery] Guid userId, CancellationToken ct)
    {
        var r = await _svc.MarkStopDroppedOffAsync(publicId, stopId, userId, ct);
        return StatusCode(r.StatusCode, r);
    }
}
