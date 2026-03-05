namespace LiventaTransfer.Application.DTOs.Driver;

public record DriverListDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public Guid VehicleOwnerId { get; init; }
    public string VehicleOwnerName { get; init; } = string.Empty;
    public bool IsActive { get; init; }

    public static DriverListDto FromEntity(Domain.Entities.Driver entity) => new()
    {
        Id = entity.Id,
        FullName = entity.FullName,
        Phone = entity.Phone,
        VehicleOwnerId = entity.VehicleOwnerId,
        VehicleOwnerName = entity.VehicleOwner?.Name ?? string.Empty,
        IsActive = entity.IsActive
    };
}

public record DriverDetailDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? WhatsAppPhone { get; init; }
    public string? LicenseNumber { get; init; }
    public Guid VehicleOwnerId { get; init; }
    public string VehicleOwnerName { get; init; } = string.Empty;
    public Guid? DefaultVehicleId { get; init; }
    public string? DefaultVehiclePlate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static DriverDetailDto FromEntity(Domain.Entities.Driver entity) => new()
    {
        Id = entity.Id,
        FullName = entity.FullName,
        Phone = entity.Phone,
        WhatsAppPhone = entity.WhatsAppPhone,
        LicenseNumber = entity.LicenseNumber,
        VehicleOwnerId = entity.VehicleOwnerId,
        VehicleOwnerName = entity.VehicleOwner?.Name ?? string.Empty,
        DefaultVehicleId = entity.DefaultVehicleId,
        DefaultVehiclePlate = entity.DefaultVehicle?.Plate,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}

public record CreateDriverRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? WhatsAppPhone { get; init; }
    public string? LicenseNumber { get; init; }
    public Guid VehicleOwnerId { get; init; }
    public Guid? DefaultVehicleId { get; init; }
}

public record UpdateDriverRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? WhatsAppPhone { get; init; }
    public string? LicenseNumber { get; init; }
    public Guid VehicleOwnerId { get; init; }
    public Guid? DefaultVehicleId { get; init; }
    public bool IsActive { get; init; }
}
