using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Auth;

public record UserInfoDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public long BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
