using LiventaTransfer.Application.DTOs.Job;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Domain.Entities;
using LiventaTransfer.Domain.Enums;
using LiventaTransfer.Tests.TestSupport;

namespace LiventaTransfer.Tests;

public class JobServiceStopSequenceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static async Task<(TestAppDbContext Db, JobService Svc, FakeJobBroadcaster Broadcaster)> CreateSutAsync()
    {
        var db = TestAppDbContext.Create();

        db.Users.Add(new User
        {
            Id = UserId,
            Username = "test",
            FirstName = "Test",
            LastName = "Kullanıcı",
            PasswordHash = "hash",
            Role = UserRole.Operations,
            BranchId = 1
        });
        db.Customers.Add(new Customer { Id = 1, Name = "Müşteri A", CustomerType = CustomerType.Individual });
        await db.SaveChangesAsync();

        var broadcaster = new FakeJobBroadcaster();
        return (db, new JobService(db, broadcaster), broadcaster);
    }

    private static JobStopRequest Stop(string note, int? sequence = null) =>
        new() { CustomerId = 1, Notes = note, Sequence = sequence };

    private static CreateJobRequest CreateRequest(params JobStopRequest[] stops) => new()
    {
        JobDate = new DateOnly(2026, 7, 20),
        JobTime = new TimeOnly(10, 30),
        JobType = JobType.Transfer,
        Stops = [.. stops]
    };

    private static Job SeedJob(TestAppDbContext db, JobStatus status = JobStatus.Open)
    {
        var job = new Job
        {
            Id = 100,
            PublicId = Guid.NewGuid(),
            JobNumber = "ERT-20260716-0001",
            JobDate = new DateOnly(2026, 7, 20),
            JobTime = new TimeOnly(10, 30),
            JobType = JobType.Transfer,
            Status = status,
            CreatedByUserId = UserId,
            Stops =
            [
                new JobStop { Id = 1, Sequence = 1, CustomerId = 1, Notes = "a" },
                new JobStop { Id = 2, Sequence = 2, CustomerId = 1, Notes = "b" },
                new JobStop { Id = 3, Sequence = 3, CustomerId = 1, Notes = "c" }
            ]
        };
        db.Jobs.Add(job);
        db.SaveChanges();
        return job;
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task Create_SequenceGonderilmemisse_ListeSirasiKullanilir()
    {
        var (_, svc, _) = await CreateSutAsync();

        var result = await svc.CreateAsync(CreateRequest(Stop("a"), Stop("b"), Stop("c")), UserId, default);

        Assert.True(result.Success);
        Assert.Equal(["a", "b", "c"], result.Data!.Stops.Select(s => s.Notes));
        Assert.Equal([1, 2, 3], result.Data!.Stops.Select(s => s.Sequence));
    }

    [Fact]
    public async Task Create_SequenceGonderilmisse_BuDegerlereGoreSiralanirVeNormalizeEdilir()
    {
        var (_, svc, _) = await CreateSutAsync();

        var result = await svc.CreateAsync(
            CreateRequest(Stop("a", 30), Stop("b", 10), Stop("c", 20)), UserId, default);

        Assert.True(result.Success);
        Assert.Equal(["b", "c", "a"], result.Data!.Stops.Select(s => s.Notes));
        Assert.Equal([1, 2, 3], result.Data!.Stops.Select(s => s.Sequence));
    }

    [Fact]
    public async Task Create_SequenceOlmayanDuraklar_SonaEklenir()
    {
        var (_, svc, _) = await CreateSutAsync();

        var result = await svc.CreateAsync(
            CreateRequest(Stop("a"), Stop("b", 2), Stop("c", 1)), UserId, default);

        Assert.True(result.Success);
        Assert.Equal(["c", "b", "a"], result.Data!.Stops.Select(s => s.Notes));
        Assert.Equal([1, 2, 3], result.Data!.Stops.Select(s => s.Sequence));
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task Update_SequenceGonderilmisse_BuDegerlereGoreSiralanir()
    {
        var (db, svc, _) = await CreateSutAsync();
        var job = SeedJob(db);

        var request = new UpdateJobRequest
        {
            JobDate = job.JobDate,
            JobTime = job.JobTime,
            JobType = job.JobType,
            Stops = [Stop("x", 2), Stop("y", 1)]
        };

        var result = await svc.UpdateAsync(job.Id, request, default);

        Assert.True(result.Success);
        Assert.Equal(["y", "x"], result.Data!.Stops.Select(s => s.Notes));
        Assert.Equal([1, 2], result.Data!.Stops.Select(s => s.Sequence));
    }

    [Fact]
    public async Task Update_SequenceGonderilmemisse_ListeSirasiKullanilir()
    {
        var (db, svc, _) = await CreateSutAsync();
        var job = SeedJob(db);

        var request = new UpdateJobRequest
        {
            JobDate = job.JobDate,
            JobTime = job.JobTime,
            JobType = job.JobType,
            Stops = [Stop("x"), Stop("y")]
        };

        var result = await svc.UpdateAsync(job.Id, request, default);

        Assert.True(result.Success);
        Assert.Equal(["x", "y"], result.Data!.Stops.Select(s => s.Notes));
        Assert.Equal([1, 2], result.Data!.Stops.Select(s => s.Sequence));
    }

    // ---------- ReorderStopsAsync ----------

    private static ReorderJobStopsRequest Reorder(params (long StopId, int Sequence)[] items) => new()
    {
        Stops = [.. items.Select(i => new JobStopSequenceItem { StopId = i.StopId, Sequence = i.Sequence })]
    };

    [Fact]
    public async Task Reorder_IsBulunamazsa_404Doner()
    {
        var (_, svc, _) = await CreateSutAsync();

        var result = await svc.ReorderStopsAsync(999, Reorder((1, 1)), default);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("İş bulunamadı.", result.Message);
    }

    [Fact]
    public async Task Reorder_BirlestirilmisIs_Duzenlenemez()
    {
        var (db, svc, _) = await CreateSutAsync();
        var job = SeedJob(db, JobStatus.Merged);

        var result = await svc.ReorderStopsAsync(job.Id, Reorder((1, 1), (2, 2), (3, 3)), default);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Birleştirilmiş işin durakları yeniden sıralanamaz.", result.Message);
    }

    [Fact]
    public async Task Reorder_IseAitOlmayanDurak_400Doner()
    {
        var (db, svc, _) = await CreateSutAsync();
        var job = SeedJob(db);

        var result = await svc.ReorderStopsAsync(job.Id, Reorder((1, 1), (2, 2), (999, 3)), default);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.StartsWith("Durak bu işe ait değil", result.Message);
    }

    [Fact]
    public async Task Reorder_TumDuraklarGonderilmezse_400Doner()
    {
        var (db, svc, _) = await CreateSutAsync();
        var job = SeedJob(db);

        var result = await svc.ReorderStopsAsync(job.Id, Reorder((1, 1), (2, 2)), default);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.StartsWith("İşin tüm durakları gönderilmelidir", result.Message);
    }

    [Fact]
    public async Task Reorder_DuplicateSequence_400Doner()
    {
        var (db, svc, _) = await CreateSutAsync();
        var job = SeedJob(db);

        var result = await svc.ReorderStopsAsync(job.Id, Reorder((1, 1), (2, 1), (3, 2)), default);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.StartsWith("Aynı sıra numarası (sequence)", result.Message);
    }

    [Fact]
    public async Task Reorder_DuplicateStopId_400Doner()
    {
        var (db, svc, _) = await CreateSutAsync();
        var job = SeedJob(db);

        var result = await svc.ReorderStopsAsync(job.Id, Reorder((1, 1), (1, 2), (3, 3)), default);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.StartsWith("Aynı durak (stopId)", result.Message);
    }

    [Fact]
    public async Task Reorder_BosListe_400Doner()
    {
        var (db, svc, _) = await CreateSutAsync();
        var job = SeedJob(db);

        var result = await svc.ReorderStopsAsync(job.Id, new ReorderJobStopsRequest(), default);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("En az bir durak (stop) gereklidir.", result.Message);
    }

    [Fact]
    public async Task Reorder_Basarili_NormalizeEderVeSiraliDetayDoner()
    {
        var (db, svc, broadcaster) = await CreateSutAsync();
        var job = SeedJob(db);

        // Aralikli (1..n olmayan) degerler gonderilir; 1..n olarak normalize edilmeli
        var result = await svc.ReorderStopsAsync(job.Id, Reorder((3, 10), (1, 30), (2, 20)), default);

        Assert.True(result.Success);
        Assert.Equal(["c", "b", "a"], result.Data!.Stops.Select(s => s.Notes));
        Assert.Equal([1, 2, 3], result.Data!.Stops.Select(s => s.Sequence));
        Assert.Equal(1, broadcaster.BroadcastCount);

        // Veritabaninda da normalize edilmis degerler kalici olmali
        var persisted = db.JobStops.Where(s => s.JobId == job.Id).ToDictionary(s => s.Id, s => s.Sequence);
        Assert.Equal(1, persisted[3]);
        Assert.Equal(2, persisted[2]);
        Assert.Equal(3, persisted[1]);
    }
}
