namespace API.Interfaces.Repositories;

public interface IBookingRepository
{
    Task<bool> HasActiveBookingsAsync(int staffId, DateOnly date, TimeOnly start, TimeOnly end);
    Task SaveChanges();
}
