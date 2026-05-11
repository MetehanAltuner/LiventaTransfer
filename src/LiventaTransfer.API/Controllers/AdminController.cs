using LiventaTransfer.Application.Common;
using LiventaTransfer.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.API.Controllers;

/// <summary>Yönetim / bakım uçları</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Admin")]
public sealed class AdminController : ControllerBase
{
    private static readonly string[] TableNames =
    [
        "Notifications",
        "InvoiceItems",
        "Invoices",
        "TripLogs",
        "JobNotes",
        "JobStatusHistories",
        "JobStops",
        "Jobs",
        "CustomerLocations",
        "Locations",
        "Drivers",
        "Vehicles",
        "VehicleOwners",
        "Passengers",
        "Customers",
        "RolePermissions",
        "Permissions",
        "Users",
        "Branches"
    ];

    private readonly AppDbContext _db;
    private readonly IServiceProvider _serviceProvider;

    public AdminController(AppDbContext db, IServiceProvider serviceProvider)
    {
        _db = db;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Tüm test verilerini siler ve seed verisini yeniden yükler.
    /// Tabloları TRUNCATE ... RESTART IDENTITY CASCADE ile temizler, ardından DataSeeder'ı çalıştırır.
    /// </summary>
    [HttpPost("reset-test-data")]
    public async Task<IActionResult> ResetTestData(CancellationToken ct)
    {
        var tableList = string.Join(", ", TableNames.Select(t => $"\"{t}\""));
        var sql = $"TRUNCATE TABLE {tableList} RESTART IDENTITY CASCADE;";

        await _db.Database.ExecuteSqlRawAsync(sql, ct);

        await DataSeeder.SeedAsync(_serviceProvider);

        return Ok(ApiResult<object>.Ok(new { TruncatedTables = TableNames.Length }, "Test verileri silindi, seed verisi yeniden yüklendi."));
    }
}
