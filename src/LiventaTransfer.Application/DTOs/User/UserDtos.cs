using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.User;

public record UserListDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public string BranchName { get; init; } = string.Empty;
    public bool IsActive { get; init; }

    public static UserListDto FromEntity(Domain.Entities.User e) => new()
    {
        Id = e.Id,
        Username = e.Username,
        FirstName = e.FirstName,
        LastName = e.LastName,
        Role = e.Role,
        RoleName = e.Role.ToString(),
        BranchName = e.Branch?.Name ?? string.Empty,
        IsActive = e.IsActive
    };
}

public record UserDetailDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public long BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static UserDetailDto FromEntity(Domain.Entities.User e) => new()
    {
        Id = e.Id,
        Username = e.Username,
        FirstName = e.FirstName,
        LastName = e.LastName,
        Role = e.Role,
        RoleName = e.Role.ToString(),
        BranchId = e.BranchId,
        BranchName = e.Branch?.Name ?? string.Empty,
        IsActive = e.IsActive,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}

public record UpdateUserRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public long BranchId { get; init; }
    public bool IsActive { get; init; }
}
