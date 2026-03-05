using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Vehicle;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Araçlar</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Araçlar")]
public sealed class VehiclesController : ControllerBase
{
    private readonly VehicleService _svc;
    public VehiclesController(VehicleService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PagedQuery query, [FromQuery] VehicleType? vehicleType, [FromQuery] Guid? vehicleOwnerId, CancellationToken ct)
    {
        var r = await _svc.GetPagedAsync(query, vehicleType, vehicleOwnerId, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _svc.GetByIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleRequest request, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken ct)
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
