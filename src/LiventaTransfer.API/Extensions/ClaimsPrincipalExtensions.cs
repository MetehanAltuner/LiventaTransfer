using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LiventaTransfer.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(sub, out var id))
            return id;

        throw new UnauthorizedAccessException("JWT 'sub' claim is missing or invalid.");
    }

    public static Guid? GetBranchId(this ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirstValue("branch_id"), out var id) ? id : null;
}
