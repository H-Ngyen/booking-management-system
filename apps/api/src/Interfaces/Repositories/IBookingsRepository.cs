using API.Constraints;
using API.Entities;

namespace API.Interfaces.Repositories;

public interface IBookingsRepository
{
    Task<(IEnumerable<Booking>?, int)> GetAllMatchAsync(DateOnly? date, BookingStatus? status, int? customerId, int pageSize, int pageNumber);
    Task<Booking?> GetById(int id);
    Task<Booking?> CreateBookingAsync(Booking entity);
    Task<bool> ExistsByCodeAsync(string bookingCode);
    Task<IEnumerable<Booking>> GetActiveBookingsAsync(int staffId, DateOnly date);
    Task<bool> HasActiveBookingsAsync(int staffId, DateOnly date, TimeOnly start, TimeOnly end);
    /// <summary>
    /// Atomically cancels overdue bookings (Pending past start, Confirmed past
    /// end) and returns the affected (booking, customer) ids for notification.
    /// </summary>
    Task<List<(int BookingId, int CustomerId)>> CancelOverdueBookingsAsync(DateTime utcNow, string reason);
    Task<bool> HasOverlapAsync(Booking entity);
    Task SaveChanges();
}
