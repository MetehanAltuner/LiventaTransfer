namespace LiventaTransfer.Application.DTOs.VehicleOwner;

public record VehicleOwnerListDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsOwnFleet { get; init; }
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }

    public static VehicleOwnerListDto FromEntity(Domain.Entities.VehicleOwner entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        IsOwnFleet = entity.IsOwnFleet,
        ContactPerson = entity.ContactPerson,
        Phone = entity.Phone,
        IsActive = entity.IsActive
    };
}

public record VehicleOwnerDetailDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsOwnFleet { get; init; }
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<VehicleBrief> Vehicles { get; init; } = [];
    public List<DriverBrief> Drivers { get; init; } = [];

    public record VehicleBrief
    {
        public long Id { get; init; }
        public string Plate { get; init; } = string.Empty;
        public string VehicleType { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }

    public record DriverBrief
    {
        public long Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }

    public static VehicleOwnerDetailDto FromEntity(Domain.Entities.VehicleOwner entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        IsOwnFleet = entity.IsOwnFleet,
        ContactPerson = entity.ContactPerson,
        Phone = entity.Phone,
        Email = entity.Email,
        Notes = entity.Notes,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Vehicles = entity.Vehicles
            .Where(v => !v.IsDeleted)
            .Select(v => new VehicleBrief
            {
                Id = v.Id,
                Plate = v.Plate,
                VehicleType = v.VehicleType.ToString(),
                IsActive = v.IsActive
            }).ToList(),
        Drivers = entity.Drivers
            .Where(d => !d.IsDeleted)
            .Select(d => new DriverBrief
            {
                Id = d.Id,
                FullName = d.FullName,
                Phone = d.Phone,
                IsActive = d.IsActive
            }).ToList()
    };
}

public record CreateVehicleOwnerRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsOwnFleet { get; init; }
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
}

public record UpdateVehicleOwnerRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsOwnFleet { get; init; }
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}
