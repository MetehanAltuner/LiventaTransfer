namespace LiventaTransfer.Application.DTOs.Lookup;

public record LookupDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record LocationLookupDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ShortCode { get; init; }
}

public record VehicleLookupDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
