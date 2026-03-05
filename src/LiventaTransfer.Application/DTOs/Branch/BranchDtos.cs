namespace LiventaTransfer.Application.DTOs.Branch;

public record BranchListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public bool IsActive { get; init; }

    public static BranchListDto FromEntity(Domain.Entities.Branch entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Address = entity.Address,
        IsActive = entity.IsActive
    };
}

public record BranchDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static BranchDetailDto FromEntity(Domain.Entities.Branch entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Address = entity.Address,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}

public record CreateBranchRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
}

public record UpdateBranchRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public bool IsActive { get; init; }
}
