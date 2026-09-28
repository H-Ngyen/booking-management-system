using API.Data;
using API.Entities;
using API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class WorkSchedulesRepository(DataContext context) : BaseRepository<WorkSchedule>(context), IWorkSchedulesRepository
{
    public async Task<IEnumerable<WorkSchedule>> GetSchedulesAsync(int staffId, DateOnly? from, DateOnly? to)
        => await NoTrackingQuery
            .Where(w => w.StaffId == staffId &&
                (from == null || w.WorkDate >= from) &&
                (to == null || w.WorkDate <= to))
            .OrderBy(w => w.WorkDate)
            .ThenBy(w => w.StartTime)
            .ToListAsync();

    public async Task<WorkSchedule?> GetScheduleById(int scheduleId)
        => await TrackingQuery.FirstOrDefaultAsync(w => w.Id == scheduleId);

    public async Task<WorkSchedule?> CreateScheduleAsync(WorkSchedule entity)
    {
        bool hasOverlap = await HasOverlappingScheduleAsync(
            entity.StaffId,
            entity.WorkDate,
            entity.StartTime,
            entity.EndTime);

        if (hasOverlap) return null;

        _dbContext.WorkSchedules.Add(entity);

        await SaveChanges();

        return entity;
    }

    public async Task DeleteSchedule(WorkSchedule entity)
    {
        _dbContext.WorkSchedules.Remove(entity);
        await SaveChanges();
    }

    public async Task<bool> HasOverlappingScheduleAsync(int staffId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeId = null)
        => await NoTrackingQuery.AnyAsync(w =>
            w.StaffId == staffId &&
            w.WorkDate == date &&
            w.StartTime < end && w.EndTime > start &&
            (excludeId == null || w.Id != excludeId));

    public async Task SaveChanges() => await _dbContext.SaveChangesAsync();
}
