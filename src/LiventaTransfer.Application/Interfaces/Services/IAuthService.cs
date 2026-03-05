using LiventaTransfer.Application.Common;
using LiventaTransfer.Application.DTOs.Auth;

namespace LiventaTransfer.Application.Interfaces.Services;

public interface IAuthService
{
    Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ApiResult<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<ApiResult<UserInfoDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
