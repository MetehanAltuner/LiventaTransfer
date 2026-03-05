using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Passenger;
using LiventaTransfer.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Yolcular</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Yolcular")]
public sealed class PassengersController : ControllerBase
{
    private readonly PassengerService _svc;
    public PassengersController(PassengerService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PagedQuery query, [FromQuery] Guid? customerId, CancellationToken ct)
    {
        var r = await _svc.GetPagedAsync(query, customerId, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var r = await _svc.GetByIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePassengerRequest request, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePassengerRequest request, CancellationToken ct)
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
