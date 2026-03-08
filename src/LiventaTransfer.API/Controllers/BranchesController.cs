using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Branch;
using LiventaTransfer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Şubeler</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Şubeler")]
public sealed class BranchesController : ControllerBase
{
    private readonly BranchService _svc;
    public BranchesController(BranchService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PagedQuery query, CancellationToken ct)
    {
        var r = await _svc.GetPagedAsync(query, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _svc.GetByIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBranchRequest request, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBranchRequest request, CancellationToken ct)
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
}
