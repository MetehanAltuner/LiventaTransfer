using System.IdentityModel.Tokens.Jwt;
using LiventaTransfer.Application.DTOs.JobNote;
using LiventaTransfer.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>İş Notları</summary>
[ApiController]
[Route("api")]
[Authorize]
[Tags("İş Notları")]
public sealed class JobNotesController : ControllerBase
{
    private readonly JobNoteService _svc;
    public JobNotesController(JobNoteService svc) => _svc = svc;

    [HttpPost("jobs/{jobId:guid}/notes")]
    public async Task<IActionResult> Create(Guid jobId, [FromBody] CreateJobNoteRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var r = await _svc.CreateAsync(jobId, request, userId.Value, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("job-notes/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobNoteRequest request, CancellationToken ct)
    {
        var r = await _svc.UpdateAsync(id, request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpDelete("job-notes/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _svc.DeleteAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
