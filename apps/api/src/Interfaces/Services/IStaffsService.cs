using API.Common;
using API.DTOs;

namespace API.Interfaces.Services;

public interface IStaffsService
{
    Task<PagedResult<StaffDto>> GetAllMatch(GetAllMatchStaffRequest request);
    Task<int> Create(CreateNewStaffRequest request);
    Task Update(UpdateStaffRequest request);
    Task<IEnumerable<WorkScheduleDto>> GetSchedules(int staffId, GetSchedulesRequest request);
    Task<WorkScheduleDto> CreateSchedule(int staffId, CreateScheduleRequest request);
    Task DeleteSchedule(int staffId, int scheduleId);
}
