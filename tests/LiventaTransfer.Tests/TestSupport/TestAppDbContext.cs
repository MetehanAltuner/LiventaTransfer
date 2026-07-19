using LiventaTransfer.Application.Interfaces;
using LiventaTransfer.Application.Interfaces.Services;
using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Tests.TestSupport;

/// <summary>
/// Servis testleri için EF Core InMemory tabanlı IAppDbContext implementasyonu.
/// </summary>
public sealed class TestAppDbContext : DbContext, IAppDbContext
{
    public TestAppDbContext(DbContextOptions<TestAppDbContext> options) : base(options) { }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<VehicleOwner> VehicleOwners => Set<VehicleOwner>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<PassengerLocation> PassengerLocations => Set<PassengerLocation>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobStop> JobStops => Set<JobStop>();
    public DbSet<JobStopPassenger> JobStopPassengers => Set<JobStopPassenger>();
    public DbSet<JobStatusHistory> JobStatusHistories => Set<JobStatusHistory>();
    public DbSet<TripLog> TripLogs => Set<TripLog>();
    public DbSet<JobNote> JobNotes => Set<JobNote>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public static TestAppDbContext Create() =>
        new(new DbContextOptionsBuilder<TestAppDbContext>()
            .UseInMemoryDatabase($"liventa-tests-{Guid.NewGuid():N}")
            .Options);
}

/// <summary>Testlerde broadcast çağrılarını sayan sahte IJobBroadcaster.</summary>
public sealed class FakeJobBroadcaster : IJobBroadcaster
{
    public int BroadcastCount { get; private set; }

    public Task BroadcastJobsChangedAsync(CancellationToken ct = default)
    {
        BroadcastCount++;
        return Task.CompletedTask;
    }
}
