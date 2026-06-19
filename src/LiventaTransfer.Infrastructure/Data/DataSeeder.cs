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

        // Yalnızca Permissions/RolePermissions dictionary'si idempotent şekilde
        // garanti edilir. Şube, kullanıcı ve diğer iş verileri sistemde zaten
        // mevcut olduğu için seed edilmez.
        await EnsurePermissionsAsync(context, logger);
    }

    /// <summary>
    /// Idempotently ensures sidebar permissions and the default role→permission map exist.
    /// Safe to run on both fresh and previously-seeded databases — adds only what is missing.
    /// </summary>
    private static async Task EnsurePermissionsAsync(AppDbContext context, ILogger logger)
    {
        var seedPerms = new (string Code, string Label, string Icon, int SortOrder)[]
        {
            ("HOME",    "Anasayfa", "home",      1),
            ("JOBS",    "İşler",    "list",      2),
            ("DETAIL",  "Detay",    "bar-chart", 3),
            ("REPORTS", "Rapor",    "bar-chart", 4),
            ("ADMIN",   "Admin",    "settings",  5),
            ("ROLE",    "Rol",      "shield",    6)
        };

        var existingCodes = await context.Permissions
            .Select(p => p.Code)
            .ToListAsync();
        var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        var addedPerms = new List<Permission>();
        foreach (var s in seedPerms)
        {
            if (existingSet.Contains(s.Code)) continue;
            addedPerms.Add(new Permission
            {
                Code = s.Code, Label = s.Label, Icon = s.Icon,
                SortOrder = s.SortOrder, IsActive = true
            });
        }
        if (addedPerms.Count > 0)
        {
            context.Permissions.AddRange(addedPerms);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} missing permission(s): {Codes}",
                addedPerms.Count, string.Join(", ", addedPerms.Select(p => p.Code)));
        }

        var permsByCode = await context.Permissions
            .ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase);

        // Default role → permission codes mapping.
        // GeneralManager/Developer are hard-coded super-admins in the service layer;
        // seeded here only so the UI matrix shows them ticked by default.
        var roleMap = new Dictionary<UserRole, string[]>
        {
            [UserRole.Operations]     = ["HOME", "JOBS", "DETAIL"],
            [UserRole.Reservation]    = ["HOME", "JOBS", "DETAIL"],
            [UserRole.Driver]         = ["HOME", "DETAIL"],
            [UserRole.Manager]        = ["HOME", "JOBS", "DETAIL", "REPORTS"],
            [UserRole.GeneralManager] = ["HOME", "JOBS", "DETAIL", "REPORTS", "ADMIN", "ROLE"],
            [UserRole.Accounting]     = ["JOBS"],
            [UserRole.Developer]      = ["HOME", "JOBS", "DETAIL", "REPORTS", "ADMIN"]
        };

        var existingPairs = await context.RolePermissions
            .Select(rp => new { rp.Role, rp.PermissionId })
            .ToListAsync();
        var existingPairSet = existingPairs
            .Select(p => (p.Role, p.PermissionId))
            .ToHashSet();

        var addedPairs = new List<RolePermission>();
        foreach (var (role, codes) in roleMap)
        {
            foreach (var code in codes)
            {
                if (!permsByCode.TryGetValue(code, out var permId)) continue;
                if (existingPairSet.Contains((role, permId))) continue;
                addedPairs.Add(new RolePermission { Role = role, PermissionId = permId });
            }
        }
        if (addedPairs.Count > 0)
        {
            context.RolePermissions.AddRange(addedPairs);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} missing role-permission pair(s).", addedPairs.Count);
        }
    }
}
