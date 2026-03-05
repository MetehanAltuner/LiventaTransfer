namespace LiventaTransfer.Application.DTOs.Auth;

public record AuthResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTime Expiration { get; init; }
    public string Username { get; init; } = string.Empty;
}
