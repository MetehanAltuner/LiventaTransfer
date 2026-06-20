using System.Text.Json;
using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Job;

namespace LiventaTransfer.Tests;

public class LenientNullableConverterTests
{
    private static readonly JsonSerializerOptions Options = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        o.Converters.Add(new LenientNullableConverterFactory());
        return o;
    }

    private static JobStopRequest Stop(string json) =>
        JsonSerializer.Deserialize<JobStopRequest>(json, Options)!;

    [Fact]
    public void EmptyString_SalePrice_BecomesNull()
    {
        var stop = Stop("""{ "customerId": 5, "salePrice": "" }""");
        Assert.Null(stop.SalePrice);
    }

    [Fact]
    public void Whitespace_SalePrice_BecomesNull()
    {
        var stop = Stop("""{ "customerId": 5, "salePrice": "   " }""");
        Assert.Null(stop.SalePrice);
    }

    [Fact]
    public void EmptyString_NullableId_BecomesNull()
    {
        var stop = Stop("""{ "customerId": 5, "pickupLocationId": "", "dropoffLocationId": "" }""");
        Assert.Null(stop.PickupLocationId);
        Assert.Null(stop.DropoffLocationId);
    }

    [Fact]
    public void StringNumber_WithDot_IsParsed()
    {
        var stop = Stop("""{ "customerId": 5, "salePrice": "150.50" }""");
        Assert.Equal(150.50m, stop.SalePrice);
    }

    [Fact]
    public void StringNumber_WithComma_IsParsed()
    {
        var stop = Stop("""{ "customerId": 5, "salePrice": "150,50" }""");
        Assert.Equal(150.50m, stop.SalePrice);
    }

    [Fact]
    public void RealNumber_StillWorks()
    {
        var stop = Stop("""{ "customerId": 5, "salePrice": 200 }""");
        Assert.Equal(200m, stop.SalePrice);
    }

    [Fact]
    public void ExplicitNull_StaysNull()
    {
        var stop = Stop("""{ "customerId": 5, "salePrice": null }""");
        Assert.Null(stop.SalePrice);
    }

    [Fact]
    public void CreateJobRequest_EmptyOptionalNumbers_BecomeNull_AndDatesParse()
    {
        var json = """
        {
            "jobDate": "2026-06-20",
            "jobTime": "14:30",
            "jobType": 1,
            "purchasePrice": "",
            "extraCost": "",
            "vehicleOwnerId": "",
            "stops": [ { "customerId": 5, "salePrice": "" } ]
        }
        """;

        var req = JsonSerializer.Deserialize<CreateJobRequest>(json, Options)!;

        Assert.Null(req.PurchasePrice);
        Assert.Null(req.ExtraCost);
        Assert.Null(req.VehicleOwnerId);
        Assert.Equal(new DateOnly(2026, 6, 20), req.JobDate);
        Assert.Equal(new TimeOnly(14, 30), req.JobTime);
        Assert.Single(req.Stops);
        Assert.Null(req.Stops[0].SalePrice);
    }
}
