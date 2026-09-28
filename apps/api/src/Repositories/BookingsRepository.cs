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
        DateTime? dayStart = date?.ToDateTime(TimeOnly.MinValue);
        DateTime? dayEnd = date?.ToDateTime(TimeOnly.MinValue).AddDays(1);

        IQueryable<Booking>? baseQuery = NoTrackingQuery
            .Include(b => b.Service)
            .Include(b => b.Customer)
            .Include(b => b.Staff)
            .Where(b => (customerId == null || b.CustomerId == customerId) &&
                (status == null || b.Status == status) &&
                (dayStart == null || (b.StartTime >= dayStart && b.StartTime < dayEnd)));

        int totalCount = await baseQuery.CountAsync();

        // long arithmetic: pageSize * (pageNumber - 1) overflows int32 for huge
        // page numbers (negative Skip => DB 2201X => 500). Clamp instead.
        long offset = (long)pageSize * (pageNumber - 1);
        int skip = offset > int.MaxValue ? int.MaxValue : (int)offset;

        IEnumerable<Booking>? bookings = await baseQuery
            .OrderByDescending(b => b.StartTime)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return (bookings, totalCount);
    }

    public async Task<Booking?> GetById(int id)
        => await TrackingQuery
            .Include(b => b.Service)
            .Include(b => b.Customer)
            .Include(b => b.Staff)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Booking?> CreateBookingAsync(Booking entity)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        // Row-level lock on the parent Staff row serializes check+insert per
        // staff. The conflicting booking row may not exist yet (phantom),
        // so the existing parent row is the lock representative.
        Staff? staff = await _dbContext.Staffs
            .FromSqlInterpolated($"SELECT * FROM \"Staffs\" WHERE \"Id\" = {entity.StaffId} FOR UPDATE")
            .FirstOrDefaultAsync();
        if (staff == null)
            throw new InvalidOperationException($"Staff {entity.StaffId} not found during booking.");

        bool hasOverlap = await HasOverlapAsync(entity);
        if (hasOverlap) return null;

        _dbContext.Bookings.Add(entity);
        await SaveChanges();

        await transaction.CommitAsync();

        return entity;
    }

    public async Task<bool> HasOverlapAsync(Booking entity)
        => await NoTrackingQuery.AnyAsync(b =>
            b.StaffId == entity.StaffId &&
            b.Status != BookingStatus.Cancelled &&
            b.StartTime < entity.EndTime && b.EndTime > entity.StartTime);

    public async Task<bool> ExistsByCodeAsync(string bookingCode)
        => await NoTrackingQuery.AnyAsync(b => b.BookingCode == bookingCode);

    public async Task<IEnumerable<Booking>> GetActiveBookingsAsync(int staffId, DateOnly date)
    {
        DateTime dayStart = date.ToDateTime(TimeOnly.MinValue);
        DateTime dayEnd = dayStart.AddDays(1);
        return await NoTrackingQuery
            .Where(b => b.StaffId == staffId &&
                b.Status != BookingStatus.Cancelled &&
                b.StartTime >= dayStart && b.StartTime < dayEnd)
            .ToListAsync();
    }

    public async Task<bool> HasActiveBookingsAsync(int staffId, DateOnly date, TimeOnly start, TimeOnly end)
    {
        DateTime shiftStart = date.ToDateTime(start);
        DateTime shiftEnd = date.ToDateTime(end);
        return await NoTrackingQuery.AnyAsync(b =>
            b.StaffId == staffId &&
            b.Status != BookingStatus.Cancelled &&
            b.StartTime < shiftEnd && b.EndTime > shiftStart);
    }

    public async Task<List<(int BookingId, int CustomerId)>> CancelOverdueBookingsAsync(DateTime utcNow, string reason)
    {
        List<(int BookingId, int CustomerId)> overdue = await NoTrackingQuery
            .Where(b => (b.Status == BookingStatus.Pending && b.StartTime <= utcNow)
                || (b.Status == BookingStatus.Confirmed && b.EndTime <= utcNow))
            .Select(b => new ValueTuple<int, int>(b.Id, b.CustomerId))
            .ToListAsync();

        if (overdue.Count == 0)
            return overdue;

        List<int> ids = overdue.Select(b => b.BookingId).ToList();
        await _dbContext.Bookings
            .Where(b => ids.Contains(b.Id)
                && ((b.Status == BookingStatus.Pending && b.StartTime <= utcNow)
                    || (b.Status == BookingStatus.Confirmed && b.EndTime <= utcNow)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, BookingStatus.Cancelled)
                .SetProperty(b => b.CancellationReason, reason)
                .SetProperty(b => b.UpdatedAt, utcNow));

        return overdue;
    }

    public async Task SaveChanges() => await _dbContext.SaveChangesAsync();
}
