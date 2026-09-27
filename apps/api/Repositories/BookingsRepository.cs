using API.Constraints;
using API.Data;
using API.Entities;
using API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class BookingsRepository(DataContext context) : BaseRepository<Booking>(context), IBookingsRepository
{
    public async Task<(IEnumerable<Booking>?, int)> GetAllMatchAsync(DateOnly? date, BookingStatus? status, int? customerId, int pageSize, int pageNumber)
    {
        DateTime? dayStart = date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime? dayEnd = date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        IQueryable<Booking>? baseQuery = NoTrackingQuery
            .Where(b => (customerId == null || b.CustomerId == customerId) &&
                (status == null || b.Status == status) &&
                (dayStart == null || (b.StartTime >= dayStart && b.StartTime < dayEnd)));

        int totalCount = await baseQuery.CountAsync();

        IEnumerable<Booking>? bookings = await baseQuery
            .OrderByDescending(b => b.StartTime)
            .Skip(pageSize * (pageNumber - 1))
            .Take(pageSize)
            .ToListAsync();

        return (bookings, totalCount);
    }

    public async Task<Booking?> GetById(int id)
        => await TrackingQuery.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Booking?> CreateBookingAsync(Booking entity)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        string lockKey = $"booking:{entity.StaffId}:{entity.StartTime:yyyy-MM-dd}";
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({lockKey}))");

        bool hasOverlap = await NoTrackingQuery.AnyAsync(b =>
            b.StaffId == entity.StaffId &&
            b.Status != BookingStatus.Cancelled &&
            b.StartTime < entity.EndTime && b.EndTime > entity.StartTime);

        if (hasOverlap) return null;

        _dbContext.Bookings.Add(entity);

        await SaveChanges();

        await transaction.CommitAsync();

        return entity;
    }

    public async Task<bool> ExistsByCodeAsync(string bookingCode)
        => await NoTrackingQuery.AnyAsync(b => b.BookingCode == bookingCode);

    public async Task<IEnumerable<Booking>> GetActiveBookingsAsync(int staffId, DateOnly date)
    {
        DateTime dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime dayEnd = dayStart.AddDays(1);
        return await NoTrackingQuery
            .Where(b => b.StaffId == staffId &&
                b.Status != BookingStatus.Cancelled &&
                b.StartTime >= dayStart && b.StartTime < dayEnd)
            .ToListAsync();
    }

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
