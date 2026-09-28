using System.Security.Claims;
using API.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

[Authorize]
public class BookingHub : Hub
{
    public const string Route = "/hubs/bookings";
    public const string MethodBookingChanged = "BookingChanged";

    public override async Task OnConnectedAsync()
    {
        // NOTE: read identity from Context.User, NOT from IUserContext:
        // IHttpContextAccessor.HttpContext is null outside a request scope
        // (LongPolling/TestServer), while Context.User is always populated
        // by the hub pipeline on every transport and server.
        string? userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        string? role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            Context.Abort();
            return;
        }

        if (string.Equals(role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnConnectedAsync();
    }
}

public record BookingChangedEvent(int BookingId, int CustomerId, string ChangeType);

public static class BookingChangeTypes
{
    public const string Created = "Created";
    public const string StatusChanged = "StatusChanged";
    public const string Cancelled = "Cancelled";
    public const string AutoCancelled = "AutoCancelled";
}
