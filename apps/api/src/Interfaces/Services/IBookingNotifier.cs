namespace API.Interfaces.Services;
public interface IBookingNotifier
{
    Task NotifyBookingChangedAsync(int bookingId, int customerId, string changeType);
}
