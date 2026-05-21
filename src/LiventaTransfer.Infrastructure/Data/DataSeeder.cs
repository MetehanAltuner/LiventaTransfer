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

        // Permissions/RolePermissions are seeded idempotently so existing
        // databases (seeded before this block existed) also get backfilled.
        await EnsurePermissionsAsync(context, logger);

        if (await context.Branches.AnyAsync())
        {
            logger.LogInformation("Database already seeded");
            return;
        }

        logger.LogInformation("Seeding database...");

        // ── Branches ──
        var branchAntalya = new Branch { Name = "Antalya Merkez", Address = "Muratpaşa, Antalya", IsActive = true };
        var branchBodrum = new Branch { Name = "Bodrum Şube", Address = "Bodrum, Muğla", IsActive = true };
        context.Branches.AddRange(branchAntalya, branchBodrum);
        await context.SaveChangesAsync();

        // ── Users (Id stays Guid) ──
        var adminUser = new User
        {
            Id = Guid.NewGuid(), Username = "admin", FirstName = "Genel", LastName = "Müdür",
            PasswordHash = BCryptHash("admin123"), Role = UserRole.GeneralManager, BranchId = branchAntalya.Id, IsActive = true
        };
        var coordUser = new User
        {
            Id = Guid.NewGuid(), Username = "operasyon", FirstName = "Ayşe", LastName = "Yılmaz",
            PasswordHash = BCryptHash("coord123"), Role = UserRole.Operations, BranchId = branchAntalya.Id, IsActive = true
        };
        var reservUser = new User
        {
            Id = Guid.NewGuid(), Username = "rezervasyon", FirstName = "Mehmet", LastName = "Kaya",
            PasswordHash = BCryptHash("reserv123"), Role = UserRole.Reservation, BranchId = branchBodrum.Id, IsActive = true
        };
        var devUser = new User
        {
            Id = Guid.NewGuid(), Username = "developer", FirstName = "Dev", LastName = "User",
            PasswordHash = BCryptHash("dev123"), Role = UserRole.Developer, BranchId = branchAntalya.Id, IsActive = true
        };
        context.Users.AddRange(adminUser, coordUser, reservUser, devUser);
        await context.SaveChangesAsync();

        // ── Vehicle Owners ──
        var ownFleet = new VehicleOwner
        {
            Name = "Liventa Filo", IsOwnFleet = true,
            ContactPerson = "Ali Demir", Phone = "05321234567", IsActive = true
        };
        var subcon1 = new VehicleOwner
        {
            Name = "Ertur Turizm", IsOwnFleet = false,
            ContactPerson = "Hasan Çelik", Phone = "05339876543", IsActive = true
        };
        var subcon2 = new VehicleOwner
        {
            Name = "Netur VIP", IsOwnFleet = false,
            ContactPerson = "Fatma Aksoy", Phone = "05445556677", IsActive = true
        };
        context.VehicleOwners.AddRange(ownFleet, subcon1, subcon2);
        await context.SaveChangesAsync();

        // ── Vehicles ──
        var v1 = new Vehicle { Plate = "07 ABC 123", VehicleType = VehicleType.Sedan, Brand = "Mercedes", Model = "E200", Year = 2023, Capacity = 4, VehicleOwnerId = ownFleet.Id, IsActive = true };
        var v2 = new Vehicle { Plate = "07 DEF 456", VehicleType = VehicleType.Vito, Brand = "Mercedes", Model = "Vito Tourer", Year = 2024, Capacity = 8, VehicleOwnerId = ownFleet.Id, IsActive = true };
        var v3 = new Vehicle { Plate = "07 GHI 789", VehicleType = VehicleType.Sprinter, Brand = "Mercedes", Model = "Sprinter", Year = 2022, Capacity = 16, VehicleOwnerId = subcon1.Id, IsActive = true };
        var v4 = new Vehicle { Plate = "48 JKL 012", VehicleType = VehicleType.Sedan, Brand = "BMW", Model = "520i", Year = 2024, Capacity = 4, VehicleOwnerId = subcon2.Id, IsActive = true };
        var v5 = new Vehicle { Plate = "48 MNO 345", VehicleType = VehicleType.Minibus, Brand = "Ford", Model = "Transit", Year = 2023, Capacity = 14, VehicleOwnerId = subcon2.Id, IsActive = true };
        context.Vehicles.AddRange(v1, v2, v3, v4, v5);
        await context.SaveChangesAsync();

        // ── Drivers ──
        var d1 = new Driver { FullName = "Ahmet Yıldırım", Phone = "05301112233", WhatsAppPhone = "05301112233", LicenseNumber = "ANT-001", VehicleOwnerId = ownFleet.Id, DefaultVehicleId = v1.Id, IsActive = true };
        var d2 = new Driver { FullName = "Mustafa Özkan", Phone = "05302223344", WhatsAppPhone = "05302223344", LicenseNumber = "ANT-002", VehicleOwnerId = ownFleet.Id, DefaultVehicleId = v2.Id, IsActive = true };
        var d3 = new Driver { FullName = "Emre Şahin", Phone = "05333334455", WhatsAppPhone = "05333334455", LicenseNumber = "ERT-001", VehicleOwnerId = subcon1.Id, DefaultVehicleId = v3.Id, IsActive = true };
        var d4 = new Driver { FullName = "Burak Koç", Phone = "05444445566", WhatsAppPhone = "05444445566", LicenseNumber = "NET-001", VehicleOwnerId = subcon2.Id, DefaultVehicleId = v4.Id, IsActive = true };
        context.Drivers.AddRange(d1, d2, d3, d4);
        await context.SaveChangesAsync();

        // ── Customers ──
        var c1 = new Customer { Name = "TUI Türkiye", CustomerType = CustomerType.Corporate, TaxNumber = "1234567890", TaxOffice = "Antalya", Phone = "02423111111", Email = "ops@tui.com.tr", IsActive = true };
        var c2 = new Customer { Name = "Coral Travel", CustomerType = CustomerType.Corporate, TaxNumber = "0987654321", TaxOffice = "Antalya", Phone = "02423222222", Email = "transfer@coraltravel.com.tr", IsActive = true };
        var c3 = new Customer { Name = "Pegas Touristik", CustomerType = CustomerType.Corporate, TaxNumber = "5678901234", TaxOffice = "Antalya", Phone = "02423333333", Email = "ops@pegasus.com.tr", IsActive = true };
        var c4 = new Customer { Name = "Fatih Çetin", CustomerType = CustomerType.Individual, TcKimlikNo = "12345678901", Phone = "05551112233", Email = "fatih@email.com", IsActive = true };
        var c5 = new Customer { Name = "Elif Arslan", CustomerType = CustomerType.Individual, TcKimlikNo = "98765432109", Phone = "05552223344", Email = "elif@email.com", IsActive = true };
        context.Customers.AddRange(c1, c2, c3, c4, c5);
        await context.SaveChangesAsync();

        // ── Passengers ──
        var p1 = new Passenger { FullName = "Hans Müller", Phone = "+491761234567", IsActive = true };
        var p2 = new Passenger { FullName = "Anna Schmidt", Phone = "+491769876543", IsActive = true };
        var p3 = new Passenger { FullName = "Ivan Petrov", Phone = "+79161234567", IsActive = true };
        var p4 = new Passenger { FullName = "Olga Ivanova", Phone = "+79169876543", IsActive = true };
        var p5 = new Passenger { FullName = "Sergei Volkov", Phone = "+79171112233", IsActive = true };
        var p6 = new Passenger { FullName = "Maria Sokolova", Phone = "+79172223344", IsActive = true };
        var p7 = new Passenger { FullName = "Fatih Çetin", Phone = "05551112233", IsActive = true };
        var p8 = new Passenger { FullName = "Elif Arslan", Phone = "05552223344", IsActive = true };
        context.Passengers.AddRange(p1, p2, p3, p4, p5, p6, p7, p8);
        await context.SaveChangesAsync();

        // ── Locations ──
        var locAYT = new Location { Name = "Antalya Havalimanı", ShortCode = "AYT", LocationType = LocationType.Airport, Latitude = 36.8987m, Longitude = 30.8005m, IsActive = true };
        var locDLM = new Location { Name = "Dalaman Havalimanı", ShortCode = "DLM", LocationType = LocationType.Airport, Latitude = 36.7131m, Longitude = 28.7925m, IsActive = true };
        var locBJV = new Location { Name = "Milas-Bodrum Havalimanı", ShortCode = "BJV", LocationType = LocationType.Airport, Latitude = 37.2506m, Longitude = 27.6643m, IsActive = true };
        var locH1 = new Location { Name = "Rixos Sungate", ShortCode = "RXS", LocationType = LocationType.Hotel, Address = "Beldibi, Kemer, Antalya", IsActive = true };
        var locH2 = new Location { Name = "Titanic Mardan Palace", ShortCode = "TMP", LocationType = LocationType.Hotel, Address = "Lara, Antalya", IsActive = true };
        var locH3 = new Location { Name = "Regnum Carya", ShortCode = "RCG", LocationType = LocationType.Hotel, Address = "Belek, Serik, Antalya", IsActive = true };
        var locH4 = new Location { Name = "Voyage Belek", ShortCode = "VBK", LocationType = LocationType.Hotel, Address = "Belek, Serik, Antalya", IsActive = true };
        var locH5 = new Location { Name = "Mandarin Oriental Bodrum", ShortCode = "MOB", LocationType = LocationType.Hotel, Address = "Cennet Koyu, Bodrum", IsActive = true };
        var locOfc = new Location { Name = "Liventa Ofis", ShortCode = "LVO", LocationType = LocationType.Office, Address = "Muratpaşa, Antalya", IsActive = true };
        var locOther = new Location { Name = "Kaleiçi", ShortCode = "KLC", LocationType = LocationType.Other, Address = "Kaleiçi, Muratpaşa, Antalya", IsActive = true };
        context.Locations.AddRange(locAYT, locDLM, locBJV, locH1, locH2, locH3, locH4, locH5, locOfc, locOther);
        await context.SaveChangesAsync();

        // ── Jobs (10 adet, farklı durumlarda) ──
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        static JobStop Stop(int seq, long customerId, long? passengerId, int paxCount, long? pickupId, long? dropoffId, string? flight = null, decimal? salePrice = null) => new()
        {
            Sequence = seq,
            CustomerId = customerId,
            PassengerId = passengerId,
            PassengerCount = paxCount,
            PickupLocationId = pickupId,
            DropoffLocationId = dropoffId,
            FlightCode = flight,
            SalePrice = salePrice
        };

        var job1 = new Job
        {
            JobNumber = "ERT-20260301-0001", JobDate = today.AddDays(-4), JobTime = new TimeOnly(14, 30),
            JobType = JobType.Transfer, Status = JobStatus.Completed,
            PurchasePrice = 100,
            VehicleOwnerId = ownFleet.Id, VehicleId = v1.Id, DriverId = d1.Id,
            CreatedByUserId = coordUser.Id,
            Stops = { Stop(1, c1.Id, p1.Id, 2, locAYT.Id, locH1.Id, "TK2414", 150) }
        };
        var job2 = new Job
        {
            JobNumber = "ERT-20260301-0002", JobDate = today.AddDays(-4), JobTime = new TimeOnly(16, 0),
            JobType = JobType.Transfer, Status = JobStatus.Completed,
            PurchasePrice = 80,
            VehicleOwnerId = ownFleet.Id, VehicleId = v2.Id, DriverId = d2.Id,
            CreatedByUserId = coordUser.Id,
            Stops = { Stop(1, c2.Id, p3.Id, 1, locAYT.Id, locH2.Id, "SU2138", 120) }
        };
        var job3 = new Job
        {
            JobNumber = "ERT-20260302-0001", JobDate = today.AddDays(-3), JobTime = new TimeOnly(9, 0),
            JobType = JobType.Transfer, Status = JobStatus.Completed,
            PurchasePrice = 100,
            VehicleOwnerId = ownFleet.Id, VehicleId = v1.Id, DriverId = d1.Id,
            CreatedByUserId = coordUser.Id,
            Stops = { Stop(1, c1.Id, p2.Id, 2, locH1.Id, locAYT.Id, "TK2415", 150) }
        };
        var job4 = new Job
        {
            JobNumber = "ERT-20260303-0001", JobDate = today.AddDays(-2), JobTime = new TimeOnly(11, 0),
            JobType = JobType.Transfer, Status = JobStatus.PendingInvoice,
            PurchasePrice = 140,
            VehicleOwnerId = subcon1.Id, VehicleId = v3.Id, DriverId = d3.Id,
            CreatedByUserId = reservUser.Id,
            Stops = { Stop(1, c3.Id, p5.Id, 4, locAYT.Id, locH3.Id, "PC1234", 200) }
        };
        var job5 = new Job
        {
            JobNumber = "ERT-20260304-0001", JobDate = today.AddDays(-1), JobTime = new TimeOnly(8, 30),
            JobType = JobType.DailyAllocation, Status = JobStatus.InProgress,
            PurchasePrice = 200,
            VehicleOwnerId = subcon2.Id, VehicleId = v4.Id, DriverId = d4.Id,
            CreatedByUserId = coordUser.Id,
            Stops = { Stop(1, c2.Id, p4.Id, 3, locH2.Id, locOther.Id, salePrice: 300) }
        };
        var job6 = new Job
        {
            JobNumber = "ERT-20260305-0001", JobDate = today, JobTime = new TimeOnly(10, 0),
            JobType = JobType.Transfer, Status = JobStatus.Assigned,
            PurchasePrice = 70,
            VehicleOwnerId = ownFleet.Id, VehicleId = v1.Id, DriverId = d1.Id,
            CreatedByUserId = reservUser.Id, AssignedByUserId = coordUser.Id,
            Stops = { Stop(1, c4.Id, p7.Id, 1, locAYT.Id, locH4.Id, "XQ1122", 100) }
        };
        // job7 — toplu iş örneği: 2 farklı müşteri/yolcu aynı araçla aynı rotada (BJV → otel)
        var job7 = new Job
        {
            JobNumber = "ERT-20260305-0002", JobDate = today, JobTime = new TimeOnly(15, 0),
            JobType = JobType.Transfer, Status = JobStatus.Open,
            CreatedByUserId = reservUser.Id,
            Stops =
            {
                Stop(1, c5.Id, p8.Id, 2, locBJV.Id, locH5.Id, "TK2554", 180),
                Stop(2, c3.Id, p6.Id, 2, locBJV.Id, locH5.Id, "TK2554", 200)
            }
        };
        var job8 = new Job
        {
            JobNumber = "ERT-20260305-0003", JobDate = today, JobTime = new TimeOnly(18, 0),
            JobType = JobType.Transfer, Status = JobStatus.Open,
            CreatedByUserId = coordUser.Id,
            Stops = { Stop(1, c3.Id, p6.Id, 2, locAYT.Id, locH3.Id, "SU2140", 200) }
        };
        var job9 = new Job
        {
            JobNumber = "ERT-20260306-0001", JobDate = today.AddDays(1), JobTime = new TimeOnly(7, 0),
            JobType = JobType.Transfer, Status = JobStatus.Assigned,
            PurchasePrice = 100,
            VehicleOwnerId = ownFleet.Id, VehicleId = v2.Id, DriverId = d2.Id,
            CreatedByUserId = coordUser.Id, AssignedByUserId = coordUser.Id,
            Stops = { Stop(1, c1.Id, p1.Id, 2, locH4.Id, locAYT.Id, "TK2417", 150) }
        };
        var job10 = new Job
        {
            JobNumber = "ERT-20260228-0001", JobDate = today.AddDays(-5), JobTime = new TimeOnly(12, 0),
            JobType = JobType.Transfer, Status = JobStatus.Cancelled,
            CreatedByUserId = reservUser.Id,
            Notes = "Müşteri iptal etti - uçuş değişikliği",
            Stops = { Stop(1, c5.Id, p8.Id, 1, locDLM.Id, locH5.Id, "PC5678", 250) }
        };
        context.Jobs.AddRange(job1, job2, job3, job4, job5, job6, job7, job8, job9, job10);
        await context.SaveChangesAsync();

        // ── Job Status Histories ──
        var histories = new List<JobStatusHistory>
        {
            new() { JobId = job1.Id, OldStatus = null, NewStatus = JobStatus.Open, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-4).AddHours(-2) },
            new() { JobId = job1.Id, OldStatus = JobStatus.Open, NewStatus = JobStatus.Assigned, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-4).AddHours(-1) },
            new() { JobId = job1.Id, OldStatus = JobStatus.Assigned, NewStatus = JobStatus.InProgress, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-4) },
            new() { JobId = job1.Id, OldStatus = JobStatus.InProgress, NewStatus = JobStatus.Completed, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-4).AddHours(2) },
            new() { JobId = job2.Id, OldStatus = null, NewStatus = JobStatus.Open, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-4).AddHours(-3) },
            new() { JobId = job2.Id, OldStatus = JobStatus.Open, NewStatus = JobStatus.Assigned, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-4).AddHours(-2) },
            new() { JobId = job2.Id, OldStatus = JobStatus.Assigned, NewStatus = JobStatus.Completed, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-4).AddHours(1) },
            new() { JobId = job4.Id, OldStatus = null, NewStatus = JobStatus.Open, ChangedByUserId = reservUser.Id, ChangedAt = now.AddDays(-2).AddHours(-3) },
            new() { JobId = job4.Id, OldStatus = JobStatus.Open, NewStatus = JobStatus.Assigned, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-2).AddHours(-2) },
            new() { JobId = job4.Id, OldStatus = JobStatus.Assigned, NewStatus = JobStatus.Completed, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-2).AddHours(1) },
            new() { JobId = job4.Id, OldStatus = JobStatus.Completed, NewStatus = JobStatus.PendingInvoice, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-1) },
            new() { JobId = job5.Id, OldStatus = null, NewStatus = JobStatus.Open, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-1).AddHours(-4) },
            new() { JobId = job5.Id, OldStatus = JobStatus.Open, NewStatus = JobStatus.Assigned, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-1).AddHours(-2) },
            new() { JobId = job5.Id, OldStatus = JobStatus.Assigned, NewStatus = JobStatus.InProgress, ChangedByUserId = coordUser.Id, ChangedAt = now.AddDays(-1) },
            new() { JobId = job6.Id, OldStatus = null, NewStatus = JobStatus.Open, ChangedByUserId = reservUser.Id, ChangedAt = now.AddHours(-6) },
            new() { JobId = job6.Id, OldStatus = JobStatus.Open, NewStatus = JobStatus.Assigned, ChangedByUserId = coordUser.Id, ChangedAt = now.AddHours(-4) },
            new() { JobId = job7.Id, OldStatus = null, NewStatus = JobStatus.Open, ChangedByUserId = reservUser.Id, ChangedAt = now.AddHours(-3) },
            new() { JobId = job8.Id, OldStatus = null, NewStatus = JobStatus.Open, ChangedByUserId = coordUser.Id, ChangedAt = now.AddHours(-2) },
            new() { JobId = job9.Id, OldStatus = null, NewStatus = JobStatus.Open, ChangedByUserId = coordUser.Id, ChangedAt = now.AddHours(-5) },
            new() { JobId = job9.Id, OldStatus = JobStatus.Open, NewStatus = JobStatus.Assigned, ChangedByUserId = coordUser.Id, ChangedAt = now.AddHours(-4) },
            new() { JobId = job10.Id, OldStatus = null, NewStatus = JobStatus.Open, ChangedByUserId = reservUser.Id, ChangedAt = now.AddDays(-5).AddHours(-3) },
            new() { JobId = job10.Id, OldStatus = JobStatus.Open, NewStatus = JobStatus.Cancelled, ChangedByUserId = reservUser.Id, ChangeReason = "Müşteri iptal etti - uçuş değişikliği", ChangedAt = now.AddDays(-5) }
        };
        context.JobStatusHistories.AddRange(histories);

        // ── Job Notes ──
        context.JobNotes.AddRange(
            new JobNote { JobId = job1.Id, NoteText = "Yolcu VIP karşılama istedi.", CreatedByUserId = coordUser.Id },
            new JobNote { JobId = job1.Id, NoteText = "Transfer sorunsuz tamamlandı.", CreatedByUserId = coordUser.Id },
            new JobNote { JobId = job4.Id, NoteText = "4 kişilik grup, ekstra bagaj var.", CreatedByUserId = reservUser.Id },
            new JobNote { JobId = job5.Id, NoteText = "Günlük tahsis - şehir turu.", CreatedByUserId = coordUser.Id },
            new JobNote { JobId = job6.Id, NoteText = "Bireysel müşteri, Belek otel transferi.", CreatedByUserId = reservUser.Id },
            new JobNote { JobId = job10.Id, NoteText = "İptal sebebi: uçuş saati değişti, yeni transfer oluşturulacak.", CreatedByUserId = reservUser.Id }
        );

        // ── Trip Logs (completed jobs) ──
        context.TripLogs.AddRange(
            new TripLog
            {
                JobId = job1.Id, DriverId = d1.Id,
                PickupTime = now.AddDays(-4).Date.AddHours(14).AddMinutes(45),
                DropoffTime = now.AddDays(-4).Date.AddHours(15).AddMinutes(30),
                StartKm = 45230, EndKm = 45285, WaitingMinutes = 15,
                FlightStatus = "Landed on time", DriverNotes = "Yolcu terminalde bekledi, sorunsuz."
            },
            new TripLog
            {
                JobId = job2.Id, DriverId = d2.Id,
                PickupTime = now.AddDays(-4).Date.AddHours(16).AddMinutes(20),
                DropoffTime = now.AddDays(-4).Date.AddHours(16).AddMinutes(50),
                StartKm = 31000, EndKm = 31025, WaitingMinutes = 20,
                FlightStatus = "Delayed 30 min", DriverNotes = "Uçuş gecikmeli geldi."
            },
            new TripLog
            {
                JobId = job3.Id, DriverId = d1.Id,
                PickupTime = now.AddDays(-3).Date.AddHours(9).AddMinutes(0),
                DropoffTime = now.AddDays(-3).Date.AddHours(9).AddMinutes(50),
                StartKm = 45285, EndKm = 45340, WaitingMinutes = 0,
                FlightStatus = "N/A - departure", DriverNotes = "Otelden zamanında alındı."
            },
            new TripLog
            {
                JobId = job4.Id, DriverId = d3.Id,
                PickupTime = now.AddDays(-2).Date.AddHours(11).AddMinutes(30),
                DropoffTime = now.AddDays(-2).Date.AddHours(12).AddMinutes(15),
                StartKm = 78500, EndKm = 78560, WaitingMinutes = 30,
                FlightStatus = "Landed on time", DriverNotes = "Grup büyük, bagaj yardımı yapıldı."
            }
        );

        // ── Invoices ──
        var inv1 = new Invoice
        {
            InvoiceNumber = "INV-202603-0001",
            CustomerId = c1.Id, InvoiceDate = today.AddDays(-1),
            PeriodStart = today.AddDays(-7), PeriodEnd = today.AddDays(-1),
            TotalAmount = 300, TaxAmount = 60, GrandTotal = 360,
            InvoiceStatus = InvoiceStatus.Sent
        };
        var inv2 = new Invoice
        {
            InvoiceNumber = "INV-202603-0002",
            CustomerId = c2.Id, InvoiceDate = today,
            PeriodStart = today.AddDays(-7), PeriodEnd = today,
            TotalAmount = 420, TaxAmount = 84, GrandTotal = 504,
            InvoiceStatus = InvoiceStatus.Draft
        };
        context.Invoices.AddRange(inv1, inv2);
        await context.SaveChangesAsync();

        // ── Invoice Items ──
        context.InvoiceItems.AddRange(
            new InvoiceItem { InvoiceId = inv1.Id, JobId = job1.Id, Description = "AYT → Rixos Sungate Transfer (2 pax)", Amount = 150 },
            new InvoiceItem { InvoiceId = inv1.Id, JobId = job3.Id, Description = "Rixos Sungate → AYT Transfer (2 pax)", Amount = 150 },
            new InvoiceItem { InvoiceId = inv2.Id, JobId = job2.Id, Description = "AYT → Titanic Mardan Palace Transfer (1 pax)", Amount = 120 },
            new InvoiceItem { InvoiceId = inv2.Id, JobId = job5.Id, Description = "Günlük tahsis - şehir turu (3 pax)", Amount = 300 }
        );

        // ── Notifications ──
        context.Notifications.AddRange(
            new Notification
            {
                JobId = job1.Id, RecipientType = RecipientType.Driver, RecipientPhone = d1.Phone,
                Channel = NotificationChannel.WhatsApp, Message = "Yeni transfer ataması: AYT → Rixos Sungate, 14:30, 2 yolcu.",
                SentAt = now.AddDays(-4).AddHours(-1), IsDelivered = true, DeliveredAt = now.AddDays(-4).AddHours(-1).AddMinutes(1)
            },
            new Notification
            {
                JobId = job6.Id, RecipientType = RecipientType.Driver, RecipientPhone = d1.Phone,
                Channel = NotificationChannel.WhatsApp, Message = "Yeni transfer ataması: AYT → Voyage Belek, 10:00, 1 yolcu. Uçuş: XQ1122",
                SentAt = now.AddHours(-4), IsDelivered = true, DeliveredAt = now.AddHours(-4).AddMinutes(2)
            },
            new Notification
            {
                JobId = job10.Id, RecipientType = RecipientType.Customer, RecipientPhone = "05552223344",
                Channel = NotificationChannel.SMS, Message = "Transfer talebiniz iptal edilmiştir. Detaylar için iletişime geçiniz.",
                SentAt = now.AddDays(-5), IsDelivered = true, DeliveredAt = now.AddDays(-5).AddMinutes(1)
            },
            new Notification
            {
                JobId = job4.Id, RecipientType = RecipientType.Accountant, RecipientUserId = adminUser.Id,
                Channel = NotificationChannel.InApp, Message = "ERT-20260303-0001 fatura bekliyor.",
                SentAt = now.AddDays(-1), IsDelivered = false
            }
        );

        await context.SaveChangesAsync();
        logger.LogInformation("Database seeded successfully with comprehensive sample data");
    }

    private static string BCryptHash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
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
