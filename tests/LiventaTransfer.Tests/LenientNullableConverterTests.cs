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

    // SalePrice iş seviyesine taşındı; nullable decimal converter davranışı Job üzerinden test edilir.
    private static decimal? JobSalePrice(string salePriceJson)
    {
        var json = $$"""
        { "jobDate": "2026-06-20", "jobTime": "14:30", "jobType": 1, "salePrice": {{salePriceJson}}, "stops": [ { "customerId": 5 } ] }
        """;
        return JsonSerializer.Deserialize<CreateJobRequest>(json, Options)!.SalePrice;
    }

    [Fact]
    public void EmptyString_SalePrice_BecomesNull()
    {
        Assert.Null(JobSalePrice("\"\""));
    }

    [Fact]
    public void Whitespace_SalePrice_BecomesNull()
    {
        Assert.Null(JobSalePrice("\"   \""));
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
        Assert.Equal(150.50m, JobSalePrice("\"150.50\""));
    }

    [Fact]
    public void StringNumber_WithComma_IsParsed()
    {
        Assert.Equal(150.50m, JobSalePrice("\"150,50\""));
    }

    [Fact]
    public void RealNumber_StillWorks()
    {
        Assert.Equal(200m, JobSalePrice("200"));
    }

    [Fact]
    public void ExplicitNull_StaysNull()
    {
        Assert.Null(JobSalePrice("null"));
    }

    [Fact]
    public void CreateJobRequest_EmptyOptionalNumbers_BecomeNull_AndDatesParse()
    {
        var json = """
        {
            "jobDate": "2026-06-20",
            "jobTime": "14:30",
            "jobType": 1,
            "salePrice": "",
            "purchasePrice": "",
            "extraCost": "",
            "vehicleOwnerId": "",
            "stops": [ { "customerId": 5 } ]
        }
        """;

        var req = JsonSerializer.Deserialize<CreateJobRequest>(json, Options)!;

        Assert.Null(req.SalePrice);
        Assert.Null(req.PurchasePrice);
        Assert.Null(req.ExtraCost);
        Assert.Null(req.VehicleOwnerId);
        Assert.Equal(new DateOnly(2026, 6, 20), req.JobDate);
        Assert.Equal(new TimeOnly(14, 30), req.JobTime);
        Assert.Single(req.Stops);
    }
}
