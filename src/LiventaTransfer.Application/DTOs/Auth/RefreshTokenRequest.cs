namespace LiventaTransfer.Application.DTOs.Auth;

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
