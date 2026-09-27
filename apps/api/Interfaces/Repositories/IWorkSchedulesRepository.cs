using API.Entities;

namespace API.Interfaces.Repositories;

public interface IWorkSchedulesRepository
{
    Task<IEnumerable<WorkSchedule>> GetSchedulesAsync(int staffId, DateOnly? from, DateOnly? to);
    Task<WorkSchedule?> GetScheduleById(int scheduleId);
    Task<WorkSchedule?> CreateScheduleAsync(WorkSchedule entity);
    Task DeleteSchedule(WorkSchedule entity);
    Task<bool> HasOverlappingScheduleAsync(int staffId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeId = null);
    Task SaveChanges();
}
