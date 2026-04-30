using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Customer;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LiventaTransfer.API.Controllers;

/// <summary>Müşteriler</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Müşteriler")]
public sealed class CustomersController : ControllerBase
{
    private readonly CustomerService _svc;
    public CustomersController(CustomerService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PagedQuery query, [FromQuery] CustomerType? customerType, [FromQuery] long? locationId, CancellationToken ct)
    {
        var r = await _svc.GetPagedAsync(query, customerType, locationId, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var r = await _svc.GetByIdAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(request, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
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

    [HttpGet("{id:long}/passengers")]
    public async Task<IActionResult> GetPassengers(long id, CancellationToken ct)
    {
        var r = await _svc.GetPassengersAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("{id:long}/locations")]
    public async Task<IActionResult> GetLocations(long id, CancellationToken ct)
    {
        var r = await _svc.GetLocationsAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPut("{id:long}/locations")]
    public async Task<IActionResult> SetLocations(long id, [FromBody] SetCustomerLocationsRequest request, CancellationToken ct)
    {
        var r = await _svc.SetLocationsAsync(id, request, ct);
        return StatusCode(r.StatusCode, r);
    }
}
