using LiventaTransfer.Application.DTOs.TripLog;
using LiventaTransfer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Sefer Kayıtları</summary>
[ApiController]
[Route("api")]
[Tags("Sefer Kayıtları")]
public sealed class TripLogsController : ControllerBase
{
    private readonly TripLogService _svc;
    public TripLogsController(TripLogService svc) => _svc = svc;

    [HttpPost("jobs/{jobId:long}/trip-log")]
    public async Task<IActionResult> Create(long jobId, [FromBody] CreateTripLogRequest request, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(jobId, request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("trip-logs/{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateTripLogRequest request, CancellationToken ct)
    {
        var r = await _svc.UpdateAsync(id, request, ct);
        return StatusCode(r.StatusCode, r);
    }
}
