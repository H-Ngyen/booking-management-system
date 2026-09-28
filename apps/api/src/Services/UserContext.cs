using System.Security.Claims;
using API.DTOs;
using API.Exceptions;
using API.Interfaces;

namespace API.Services;

public class UserContext(
    IHttpContextAccessor httpContextAccessor
) : IUserContext
{
    public CurrentUser GetCurrentUser()
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user == null)
            throw new InvalidOperationException(
                "User context is not present"
            );

        if (user.Identity?.IsAuthenticated != true)
            throw new UnauthorizedException();

        string userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        string role = user.FindFirst(ClaimTypes.Role)!.Value;

        if (!int.TryParse(userIdValue, out var userId) ||
            string.IsNullOrWhiteSpace(role))
        {
            throw new UnauthorizedException();
        }

        return new CurrentUser(userId, role);
    }
}