using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.DTOs.Auth;

public record RegisterRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public Guid BranchId { get; init; }
}
