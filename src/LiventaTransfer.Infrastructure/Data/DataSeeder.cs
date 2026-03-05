using LiventaTransfer.Domain.Entities;
using LiventaTransfer.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LiventaTransfer.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await context.Database.MigrateAsync();

        if (await context.Branches.AnyAsync())
        {
            logger.LogInformation("Database already seeded");
            return;
        }

        logger.LogInformation("Seeding database...");

        // Branch
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Merkez Ofis",
            IsActive = true
        };
        context.Branches.Add(branch);

        // Admin User
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = BCryptHash("admin123"),
            Role = UserRole.Admin,
            BranchId = branch.Id,
            IsActive = true
        };
        context.Users.Add(adminUser);

        // Vehicle Owners
        var vehicleOwners = new List<VehicleOwner>
        {
            new() { Id = Guid.NewGuid(), Name = "Ertur", IsOwnFleet = true, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "İnternasyonel", IsOwnFleet = false, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "DC Grup", IsOwnFleet = false, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Fibi", IsOwnFleet = false, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Netur", IsOwnFleet = false, IsActive = true }
        };
        context.VehicleOwners.AddRange(vehicleOwners);

        // Locations
        var locations = new List<Location>
        {
            new() { Id = Guid.NewGuid(), Name = "ESB", ShortCode = "ESB", LocationType = LocationType.Office, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "İstanbul Sabiha Gökçen", ShortCode = "SAW", LocationType = LocationType.Airport, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "İstanbul Havalimanı", ShortCode = "IST", LocationType = LocationType.Airport, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Ankara Esenboğa", ShortCode = "ESB-AP", LocationType = LocationType.Airport, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Adnan Menderes", ShortCode = "ADB", LocationType = LocationType.Airport, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "YHT Gar Ankara", ShortCode = "YHT", LocationType = LocationType.TrainStation, IsActive = true }
        };
        context.Locations.AddRange(locations);

        await context.SaveChangesAsync();
        logger.LogInformation("Database seeded successfully");
    }

    private static string BCryptHash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}
