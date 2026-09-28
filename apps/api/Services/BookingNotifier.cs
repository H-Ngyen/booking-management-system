using API.Hubs;
using API.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace API.Services;

public class BookingNotifier(IHubContext<BookingHub> hubContext, ILogger<BookingNotifier> logger) : IBookingNotifier
{
    public async Task NotifyBookingChangedAsync(int bookingId, int customerId, string changeType)
    {
        try
        {
            var payload = new BookingChangedEvent(bookingId, customerId, changeType);
            await hubContext.Clients.Group("admins")
                .SendAsync(BookingHub.MethodBookingChanged, payload);
            await hubContext.Clients.Group($"user:{customerId}")
                .SendAsync(BookingHub.MethodBookingChanged, payload);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push realtime update for booking {BookingId}", bookingId);
        }
    }
}
