using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Location;
using LiventaTransfer.Application.Services;
using LiventaTransfer.Domain.Entities;
using LiventaTransfer.Domain.Enums;
using LiventaTransfer.Tests.TestSupport;

namespace LiventaTransfer.Tests;

public class LocationServiceTests
{
    private static async Task<(TestAppDbContext Db, LocationService Svc)> CreateSutAsync()
    {
        var db = TestAppDbContext.Create();

        db.Locations.AddRange(
            new Location { Id = 1, Name = "Esenboğa Havalimanı", Address = "Balıkhisar Mah. Özal Bulvarı, Akyurt/Ankara", LocationType = LocationType.Airport, IsActive = true },
            new Location { Id = 2, Name = "Batıkent", Address = null, LocationType = LocationType.Residence, IsActive = true },
            new Location { Id = 3, Name = "Aselsan Gölbaşı", Address = "Gölbaşı/Ankara", LocationType = LocationType.Office, IsActive = false });
        await db.SaveChangesAsync();

        return (db, new LocationService(db));
    }

    [Fact]
    public async Task GetPaged_ListeDtolarindaAdresDoner()
    {
        var (_, svc) = await CreateSutAsync();

        var result = await svc.GetPagedAsync(new PagedQuery { Page = 1, PageSize = 10 }, null, null, default);

        Assert.True(result.Success);
        var esenboga = Assert.Single(result.Data!.Items, l => l.Id == 1);
        Assert.Equal("Balıkhisar Mah. Özal Bulvarı, Akyurt/Ankara", esenboga.Address);
    }

    [Fact]
    public async Task GetPaged_AramaAdresUzerindenEslesir()
    {
        var (_, svc) = await CreateSutAsync();

        var result = await svc.GetPagedAsync(new PagedQuery { Page = 1, PageSize = 10, Search = "akyurt" }, null, null, default);

        Assert.True(result.Success);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal(1, item.Id);
    }

    [Fact]
    public async Task GetPaged_AramaIsimUzerindenEslesir()
    {
        var (_, svc) = await CreateSutAsync();

        var result = await svc.GetPagedAsync(new PagedQuery { Page = 1, PageSize = 10, Search = "batıkent" }, null, null, default);

        Assert.True(result.Success);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal(2, item.Id);
    }

    [Fact]
    public async Task Create_AdresKaydedilirVeDetayDtosundaDoner()
    {
        var (db, svc) = await CreateSutAsync();

        var result = await svc.CreateAsync(new CreateLocationRequest
        {
            Name = "  Sabiha Gökçen  ",
            Address = "  Pendik/İstanbul  ",
            LocationType = LocationType.Airport
        }, default);

        Assert.True(result.Success);
        Assert.Equal("Sabiha Gökçen", result.Data!.Name);
        Assert.Equal("Pendik/İstanbul", result.Data!.Address);

        var entity = db.Locations.Single(l => l.Id == result.Data!.Id);
        Assert.Equal("Pendik/İstanbul", entity.Address);
    }

    [Fact]
    public async Task Update_AdresGuncellenir()
    {
        var (db, svc) = await CreateSutAsync();

        var result = await svc.UpdateAsync(2, new UpdateLocationRequest
        {
            Name = "Batıkent",
            Address = "Yenimahalle/Ankara",
            LocationType = LocationType.Residence,
            IsActive = true
        }, default);

        Assert.True(result.Success);
        Assert.Equal("Yenimahalle/Ankara", result.Data!.Address);
        Assert.Equal("Yenimahalle/Ankara", db.Locations.Single(l => l.Id == 2).Address);
    }
}
