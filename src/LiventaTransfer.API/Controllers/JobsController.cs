using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Job;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Transfer İşleri</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Transfer İşleri")]
public sealed class JobsController : ControllerBase
{
    private readonly JobService _svc;
    public JobsController(JobService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] PagedQuery query,
        [FromQuery] JobStatus? status,
        [FromQuery] long? customerId,
        [FromQuery] long? driverId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken ct)
    {
        var r = await _svc.GetPagedAsync(query, status, customerId, driverId, dateFrom, dateTo, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var r = await _svc.GetByIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request, [FromQuery] Guid userId, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(request, userId, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateJobRequest request, CancellationToken ct)
    {
        var r = await _svc.UpdateAsync(id, request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var r = await _svc.DeleteAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateJobStatusRequest request, [FromQuery] Guid userId, CancellationToken ct)
    {
        var r = await _svc.UpdateStatusAsync(id, request, userId, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}/notes")]
    public async Task<IActionResult> GetNotes(long id, CancellationToken ct)
    {
        var noteSvc = HttpContext.RequestServices.GetRequiredService<JobNoteService>();
        var r = await noteSvc.GetByJobIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}/status-history")]
    public async Task<IActionResult> GetStatusHistory(long id, CancellationToken ct)
    {
        var r = await _svc.GetStatusHistoryAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}/trip-log")]
    public async Task<IActionResult> GetTripLog(long id, CancellationToken ct)
    {
        var tripSvc = HttpContext.RequestServices.GetRequiredService<TripLogService>();
        var r = await tripSvc.GetByJobIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

}
