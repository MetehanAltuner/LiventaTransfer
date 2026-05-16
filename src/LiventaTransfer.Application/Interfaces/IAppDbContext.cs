using LiventaTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LiventaTransfer.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Branch> Branches { get; }
    DbSet<User> Users { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Passenger> Passengers { get; }
    DbSet<VehicleOwner> VehicleOwners { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<Driver> Drivers { get; }
    DbSet<Location> Locations { get; }
    DbSet<PassengerLocation> PassengerLocations { get; }
    DbSet<Job> Jobs { get; }
    DbSet<JobStop> JobStops { get; }
    DbSet<JobStatusHistory> JobStatusHistories { get; }
    DbSet<TripLog> TripLogs { get; }
    DbSet<JobNote> JobNotes { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
