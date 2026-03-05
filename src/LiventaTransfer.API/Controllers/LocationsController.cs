using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Location;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Lokasyonlar</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Lokasyonlar")]
public sealed class LocationsController : ControllerBase
{
    private readonly LocationService _svc;
    public LocationsController(LocationService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PagedQuery query, [FromQuery] LocationType? locationType, CancellationToken ct)
    {
        var r = await _svc.GetPagedAsync(query, locationType, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _svc.GetByIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLocationRequest request, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLocationRequest request, CancellationToken ct)
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
