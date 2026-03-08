using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Vehicle;

public record VehicleListDto
{
    public long Id { get; init; }
    public string Plate { get; init; } = string.Empty;
    public VehicleType VehicleType { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int Capacity { get; init; }
    public long VehicleOwnerId { get; init; }
    public string VehicleOwnerName { get; init; } = string.Empty;
    public bool IsActive { get; init; }

    public static VehicleListDto FromEntity(Domain.Entities.Vehicle entity) => new()
    {
        Id = entity.Id,
        Plate = entity.Plate,
        VehicleType = entity.VehicleType,
        Brand = entity.Brand,
        Model = entity.Model,
        Capacity = entity.Capacity,
        VehicleOwnerId = entity.VehicleOwnerId,
        VehicleOwnerName = entity.VehicleOwner?.Name ?? string.Empty,
        IsActive = entity.IsActive
    };
}

public record VehicleDetailDto
{
    public long Id { get; init; }
    public string Plate { get; init; } = string.Empty;
    public VehicleType VehicleType { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public int Capacity { get; init; }
    public long VehicleOwnerId { get; init; }
    public string VehicleOwnerName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static VehicleDetailDto FromEntity(Domain.Entities.Vehicle entity) => new()
    {
        Id = entity.Id,
        Plate = entity.Plate,
        VehicleType = entity.VehicleType,
        Brand = entity.Brand,
        Model = entity.Model,
        Year = entity.Year,
        Capacity = entity.Capacity,
        VehicleOwnerId = entity.VehicleOwnerId,
        VehicleOwnerName = entity.VehicleOwner?.Name ?? string.Empty,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}

public record CreateVehicleRequest
{
    public string Plate { get; init; } = string.Empty;
    public VehicleType VehicleType { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public int Capacity { get; init; } = 4;
    public long VehicleOwnerId { get; init; }
}

public record UpdateVehicleRequest
{
    public string Plate { get; init; } = string.Empty;
    public VehicleType VehicleType { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public int Capacity { get; init; } = 4;
    public long VehicleOwnerId { get; init; }
    public bool IsActive { get; init; }
}
