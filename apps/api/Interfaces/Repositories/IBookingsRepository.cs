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
    Task SaveChanges();
}
