using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Lookup;
using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.API.Controllers;

/// <summary>Lookup'lar</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Lookup'lar")]
public sealed class LookupsController : ControllerBase
{
    private readonly IAppDbContext _db;
    public LookupsController(IAppDbContext db) => _db = db;

    [HttpGet("customers")]
    public async Task<IActionResult> Customers(CancellationToken ct)
    {
        var items = await _db.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new LookupDto { Id = c.Id, Name = c.Name })
            .ToListAsync(ct);

        return Ok(ApiResult<List<LookupDto>>.Ok(items, "Müşteriler."));
    }

    [HttpGet("vehicle-owners")]
    public async Task<IActionResult> VehicleOwners(CancellationToken ct)
    {
        var items = await _db.VehicleOwners
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .Select(v => new LookupDto { Id = v.Id, Name = v.Name })
            .ToListAsync(ct);

        return Ok(ApiResult<List<LookupDto>>.Ok(items, "Araç sahipleri."));
    }

    [HttpGet("vehicles")]
    public async Task<IActionResult> Vehicles(CancellationToken ct)
    {
        var items = await _db.Vehicles
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.Plate)
            .Select(v => new VehicleLookupDto
            {
                Id = v.Id,
                Name = v.Plate + " - " + v.VehicleType.ToString()
            })
            .ToListAsync(ct);

        return Ok(ApiResult<List<VehicleLookupDto>>.Ok(items, "Araçlar."));
    }

    [HttpGet("drivers")]
    public async Task<IActionResult> Drivers(CancellationToken ct)
    {
        var items = await _db.Drivers
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.FullName)
            .Select(d => new LookupDto { Id = d.Id, Name = d.FullName })
            .ToListAsync(ct);

        return Ok(ApiResult<List<LookupDto>>.Ok(items, "Şoförler."));
    }

    [HttpGet("locations")]
    public async Task<IActionResult> Locations(CancellationToken ct)
    {
        var items = await _db.Locations
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .Select(l => new LocationLookupDto { Id = l.Id, Name = l.Name, ShortCode = l.ShortCode })
            .ToListAsync(ct);

        return Ok(ApiResult<List<LocationLookupDto>>.Ok(items, "Lokasyonlar."));
    }

    [HttpGet("passengers")]
    public async Task<IActionResult> Passengers([FromQuery] long? customerId, CancellationToken ct)
    {
        var q = _db.Passengers.AsNoTracking().Where(p => p.IsActive);

        if (customerId.HasValue)
            q = q.Where(p => p.CustomerId == customerId.Value);

        var items = await q
            .OrderBy(p => p.FullName)
            .Select(p => new LookupDto { Id = p.Id, Name = p.FullName })
            .ToListAsync(ct);

        return Ok(ApiResult<List<LookupDto>>.Ok(items, "Yolcular."));
    }

    [HttpGet("branches")]
    public async Task<IActionResult> Branches(CancellationToken ct)
    {
        var items = await _db.Branches
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new LookupDto { Id = b.Id, Name = b.Name })
            .ToListAsync(ct);

        return Ok(ApiResult<List<LookupDto>>.Ok(items, "Şubeler."));
    }

    [HttpGet("roles")]
    public IActionResult Roles()
    {
        var items = Enum.GetValues<UserRole>()
            .Select(r => new LookupDto { Id = (long)r, Name = r.ToString() })
            .OrderBy(r => r.Id)
            .ToList();

        return Ok(ApiResult<List<LookupDto>>.Ok(items, "Roller."));
    }

    [HttpGet("roles/{id:int}")]
    public IActionResult RoleById(int id)
    {
        if (!Enum.IsDefined(typeof(UserRole), id))
            return Ok(ApiResult<LookupDto>.Fail("Rol bulunamadı.", statusCode: 404));

        var role = (UserRole)id;
        return Ok(ApiResult<LookupDto>.Ok(new LookupDto { Id = id, Name = role.ToString() }, "Rol bulundu."));
    }
}
