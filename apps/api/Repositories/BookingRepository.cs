using API.Constraints;
using API.Data;
using API.Entities;
using API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class BookingRepository(DataContext context) : BaseRepository<Booking>(context), IBookingRepository
{
    public async Task<bool> HasActiveBookingsAsync(int staffId, DateOnly date, TimeOnly start, TimeOnly end)
    {
        DateTime shiftStart = date.ToDateTime(start, DateTimeKind.Utc);
        DateTime shiftEnd = date.ToDateTime(end, DateTimeKind.Utc);
        return await NoTrackingQuery.AnyAsync(b =>
            b.StaffId == staffId &&
            b.Status != BookingStatus.Cancelled &&
            b.StartTime < shiftEnd && b.EndTime > shiftStart);
    }

    public async Task SaveChanges() => await _dbContext.SaveChangesAsync();
}
