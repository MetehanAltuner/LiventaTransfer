namespace LiventaTransfer.Application.DTOs.Passenger;

public record PassengerListDto
{
    public long Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public bool IsActive { get; init; }

    public static PassengerListDto FromEntity(Domain.Entities.Passenger entity) => new()
    {
        Id = entity.Id,
        FullName = entity.FullName,
        Phone = entity.Phone,
        Email = entity.Email,
        CustomerId = entity.CustomerId,
        CustomerName = entity.Customer?.Name ?? string.Empty,
        IsActive = entity.IsActive
    };
}

public record PassengerDetailDto
{
    public long Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static PassengerDetailDto FromEntity(Domain.Entities.Passenger entity) => new()
    {
        Id = entity.Id,
        FullName = entity.FullName,
        Phone = entity.Phone,
        Email = entity.Email,
        Notes = entity.Notes,
        CustomerId = entity.CustomerId,
        CustomerName = entity.Customer?.Name ?? string.Empty,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}

public record CreatePassengerRequest
{
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public long CustomerId { get; init; }
}

public record UpdatePassengerRequest
{
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }
    public long CustomerId { get; init; }
    public bool IsActive { get; init; }
}
