using System.IdentityModel.Tokens.Jwt;
using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Job;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Transfer İşleri</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Transfer İşleri")]
public sealed class JobsController : ControllerBase
{
    private readonly JobService _svc;
    public JobsController(JobService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] PagedQuery query,
        [FromQuery] JobStatus? status,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? driverId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken ct)
    {
        var r = await _svc.GetPagedAsync(query, status, customerId, driverId, dateFrom, dateTo, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _svc.GetByIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var r = await _svc.CreateAsync(request, userId.Value, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobRequest request, CancellationToken ct)
    {
        var r = await _svc.UpdateAsync(id, request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _svc.DeleteAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateJobStatusRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var r = await _svc.UpdateStatusAsync(id, request, userId.Value, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:guid}/notes")]
    public async Task<IActionResult> GetNotes(Guid id, CancellationToken ct)
    {
        var noteSvc = HttpContext.RequestServices.GetRequiredService<JobNoteService>();
        var r = await noteSvc.GetByJobIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:guid}/status-history")]
    public async Task<IActionResult> GetStatusHistory(Guid id, CancellationToken ct)
    {
        var r = await _svc.GetStatusHistoryAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:guid}/trip-log")]
    public async Task<IActionResult> GetTripLog(Guid id, CancellationToken ct)
    {
        var tripSvc = HttpContext.RequestServices.GetRequiredService<TripLogService>();
        var r = await tripSvc.GetByJobIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
