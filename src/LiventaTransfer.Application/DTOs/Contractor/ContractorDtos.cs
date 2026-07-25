using LiventaTransfer.Application.Common;

namespace LiventaTransfer.Application.DTOs.Contractor;

public record ContractorListDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }

    public static ContractorListDto FromEntity(Domain.Entities.Contractor entity) => new()
    {
        Id = entity.Id,
        Name = NameFormatter.ToTitleCase(entity.Name),
        ContactPerson = entity.ContactPerson,
        Phone = entity.Phone,
        Email = entity.Email,
        IsActive = entity.IsActive
    };
}

public record ContractorDetailDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static ContractorDetailDto FromEntity(Domain.Entities.Contractor entity) => new()
    {
        Id = entity.Id,
        Name = NameFormatter.ToTitleCase(entity.Name),
        ContactPerson = entity.ContactPerson,
        Phone = entity.Phone,
        Email = entity.Email,
        Address = entity.Address,
        Notes = entity.Notes,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}

public record CreateContractorRequest
{
    public string Name { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
}

public record UpdateContractorRequest
{
    public string Name { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}
