using LiventaTransfer.Application.Common;
using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Location;

public record LocationListDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ShortCode { get; init; }
    public LocationType LocationType { get; init; }
    public string LocationTypeLabel { get; init; } = string.Empty;
    public bool IsActive { get; init; }

    public static LocationListDto FromEntity(Domain.Entities.Location entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        ShortCode = entity.ShortCode,
        LocationType = entity.LocationType,
        LocationTypeLabel = EnumLabelHelper.GetLabel(entity.LocationType),
        IsActive = entity.IsActive
    };
}

public record LocationDetailDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ShortCode { get; init; }
    public string? Address { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public LocationType LocationType { get; init; }
    public string LocationTypeLabel { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static LocationDetailDto FromEntity(Domain.Entities.Location entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        ShortCode = entity.ShortCode,
        Address = entity.Address,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        LocationType = entity.LocationType,
        LocationTypeLabel = EnumLabelHelper.GetLabel(entity.LocationType),
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}

public record CreateLocationRequest
{
    public string Name { get; init; } = string.Empty;
    public string? ShortCode { get; init; }
    public string? Address { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public LocationType LocationType { get; init; }
}

public record UpdateLocationRequest
{
    public string Name { get; init; } = string.Empty;
    public string? ShortCode { get; init; }
    public string? Address { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public LocationType LocationType { get; init; }
    public bool IsActive { get; init; }
}
