using LiventaTransfer.Application.DTOs.EmlImport;
using LiventaTransfer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>EML Mail Import</summary>
[ApiController]
[Route("api/eml-import")]
[Tags("EML Import")]
public sealed class EmlImportController : ControllerBase
{
    private readonly EmlImportService _svc;
    public EmlImportController(EmlImportService svc) => _svc = svc;

    [HttpPost("parse")]
    public async Task<IActionResult> Parse(IFormFile emlFile, CancellationToken ct)
    {
        if (emlFile == null || emlFile.Length == 0)
            return BadRequest(new { Success = false, Message = "EML dosyası gereklidir." });

        using var stream = emlFile.OpenReadStream();
        var r = await _svc.ParseAsync(stream, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmEmlImportRequest request, CancellationToken ct)
    {
        var r = await _svc.ConfirmAndCreateJobsAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }
}
