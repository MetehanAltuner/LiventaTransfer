namespace LiventaTransfer.Application.DTOs.Lookup;

public record LookupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record LocationLookupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ShortCode { get; init; }
}

public record VehicleLookupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
