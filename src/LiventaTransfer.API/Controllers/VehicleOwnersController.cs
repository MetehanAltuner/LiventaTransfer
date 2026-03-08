using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.VehicleOwner;
using LiventaTransfer.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Araç Sahipleri</summary>
[ApiController]
[Route("api/vehicle-owners")]
[Tags("Araç Sahipleri")]
public sealed class VehicleOwnersController : ControllerBase
{
    private readonly VehicleOwnerService _svc;
    public VehicleOwnersController(VehicleOwnerService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PagedQuery query, [FromQuery] bool? isOwnFleet, CancellationToken ct)
    {
        var r = await _svc.GetPagedAsync(query, isOwnFleet, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var r = await _svc.GetByIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleOwnerRequest request, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateVehicleOwnerRequest request, CancellationToken ct)
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

    [HttpGet("{id:long}/vehicles")]
    public async Task<IActionResult> GetVehicles(long id, CancellationToken ct)
    {
        var r = await _svc.GetVehiclesAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}/drivers")]
    public async Task<IActionResult> GetDrivers(long id, CancellationToken ct)
    {
        var r = await _svc.GetDriversAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }
}
