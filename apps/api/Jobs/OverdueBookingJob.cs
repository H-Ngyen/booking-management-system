using API.Common;
using API.Hubs;
using API.Interfaces.Repositories;
using API.Interfaces.Services;

namespace API.Jobs;

/// <summary>
/// Daily cleanup (0h00 UTC): auto-cancel bookings past their deadline.
/// - Pending past StartTime (never confirmed) -> Cancelled.
/// - Confirmed past EndTime (finished but never completed) -> Cancelled.
/// Pure orchestrator: data access lives in the repository, notification in
/// the notifier. The update itself is idempotent: re-running changes nothing.
/// </summary>
public class OverdueBookingJob(IBookingsRepository bookings, IBookingNotifier notifier)
{
    public const string AutoCancelReason = "Hệ thống tự động hủy do quá hạn.";

    public async Task ExecuteAsync()
    {
        List<(int BookingId, int CustomerId)> affected =
            await bookings.CancelOverdueBookingsAsync(VnClock.Now, AutoCancelReason);

        foreach (var (bookingId, customerId) in affected)
            await notifier.NotifyBookingChangedAsync(bookingId, customerId, BookingChangeTypes.AutoCancelled);
    }
}
