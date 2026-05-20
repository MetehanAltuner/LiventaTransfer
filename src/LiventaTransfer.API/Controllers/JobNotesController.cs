using LiventaTransfer.API.Extensions;
using LiventaTransfer.Application.DTOs.JobNote;
using LiventaTransfer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>İş Notları</summary>
[ApiController]
[Route("api")]
[Tags("İş Notları")]
public sealed class JobNotesController : ControllerBase
{
    private readonly JobNoteService _svc;
    public JobNotesController(JobNoteService svc) => _svc = svc;

    [HttpPost("jobs/{jobId:long}/notes")]
    public async Task<IActionResult> Create(long jobId, [FromBody] CreateJobNoteRequest request, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(jobId, request, User.GetUserId(), ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("job-notes/{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateJobNoteRequest request, CancellationToken ct)
    {
        var r = await _svc.UpdateAsync(id, request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpDelete("job-notes/{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var r = await _svc.DeleteAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }
}
