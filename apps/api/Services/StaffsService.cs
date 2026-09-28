using API.Common;
using API.Constraints;
using API.DTOs;
using API.Entities;
using API.Exceptions;
using API.Interfaces;
using API.Interfaces.Authorization;
using API.Interfaces.Repositories;
using API.Interfaces.Services;
using AutoMapper;

namespace API.Services;

public class StaffsService(IStaffsRepository staffsRepository,
    IWorkSchedulesRepository workSchedulesRepository,
    IBookingsRepository bookingsRepository,
    IMapper mapper,
    IUserContext userContext,
    IUserRepository userRepository,
    IStaffsAuthorization staffsAuthorization) : IStaffsService
{
    public async Task<int> Create(CreateNewStaffRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User user = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();

        if (!staffsAuthorization.Authorize(user, ResourceOperation.Create))
            throw new ForbidException();

        bool isStaffExisting = await staffsRepository.ExistsByEmailAsync(request.Email);
        if (isStaffExisting)
            throw new ConflictException("Email nhân viên đã tồn tại.", "STAFF_EMAIL_EXISTS");

        Staff newStaff = CreateNewStaff(request);
        newStaff = await staffsRepository.CreateAsync(newStaff) ?? throw new BadRequestException();
        return newStaff.Id;
    }

    public async Task<PagedResult<StaffDto>> GetAllMatch(GetAllMatchStaffRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();

        bool isAdmin = currentUser.Role == nameof(UserRole.Admin);
        (IEnumerable<Staff>? staffs, int totalCount) = await staffsRepository.GetAllMatchAsync(
            request.SearchPhrase,
            request.PageSize,
            request.PageNumber,
            isAdmin);

        var staffDtos = mapper.Map<IEnumerable<StaffDto>>(staffs);
        var result = new PagedResult<StaffDto>(staffDtos, totalCount, request.PageSize, request.PageNumber);

        return result;
    }

    public async Task Update(UpdateStaffRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User user = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();

        if (!staffsAuthorization.Authorize(user, ResourceOperation.Update))
            throw new ForbidException();

        Staff staff = await staffsRepository.GetById(request.Id) ?? throw new NotFoundException();

        bool isExisting = await staffsRepository.ExistsByEmailAsync(request.Email, request.Id);
        if (isExisting)
            throw new ConflictException("Email nhân viên đã tồn tại.", "STAFF_EMAIL_EXISTS");

        mapper.Map(request, staff);
        staff.UpdatedAt = VnClock.Now;

        await staffsRepository.SaveChanges();
    }

    public async Task<IEnumerable<WorkScheduleDto>> GetSchedules(int staffId, GetSchedulesRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User user = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();

        bool isExisting = await staffsRepository.ExistsById(staffId);
        if (!isExisting) throw new NotFoundException("Không tìm thấy nhân viên", "STAFF_NOT_FOUND");

        if (!staffsAuthorization.Authorize(user, ResourceOperation.Read))
            throw new ForbidException();

        IEnumerable<WorkSchedule> schedules = await workSchedulesRepository.GetSchedulesAsync(staffId, request.From, request.To);

        var results = mapper.Map<IEnumerable<WorkScheduleDto>>(schedules);
        return results;
    }

    public async Task<WorkScheduleDto> CreateSchedule(int staffId, CreateScheduleRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User user = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();

        Staff staff = await staffsRepository.GetById(staffId) ?? throw new NotFoundException("Không tìm thấy nhân viên.", "STAFF_NOT_FOUND");

        if (!staffsAuthorization.Authorize(user, ResourceOperation.Create, staff))
            throw new ForbidException();

        DateTime now = VnClock.Now;
        WorkSchedule? schedule = await workSchedulesRepository.CreateScheduleAsync(new WorkSchedule
        {
            StaffId = staffId,
            WorkDate = request.WorkDate!.Value,
            StartTime = request.StartTime!.Value,
            EndTime = request.EndTime!.Value,
            CreatedAt = now,
            UpdatedAt = now,
        });

        if (schedule == null)
            throw new ConflictException("Ca làm việc bị trùng với ca đã có của nhân viên này.", "SCHEDULE_OVERLAP");

        var result = mapper.Map<WorkScheduleDto>(schedule);
        return result;
    }

    public async Task DeleteSchedule(int staffId, int scheduleId)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User user = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();

        bool isExisting = await staffsRepository.ExistsById(staffId);
        if (!isExisting)
            throw new NotFoundException("Không tìm thấy nhân viên.", "STAFF_NOT_FOUND");

        if (!staffsAuthorization.Authorize(user, ResourceOperation.Delete))
            throw new ForbidException();

        WorkSchedule? schedule = await workSchedulesRepository.GetScheduleById(scheduleId);
        if (schedule == null || schedule.StaffId != staffId)
            throw new NotFoundException("Không tìm thấy ca làm việc.", "SCHEDULE_NOT_FOUND");

        if (await bookingsRepository.HasActiveBookingsAsync(staffId, schedule.WorkDate, schedule.StartTime, schedule.EndTime))
            throw new ConflictException("Không thể xóa ca làm việc đã có booking.", "SCHEDULE_HAS_BOOKINGS");

        await workSchedulesRepository.DeleteSchedule(schedule);
    }
    private Staff CreateNewStaff(CreateNewStaffRequest request)
    {
        Staff newStaff = mapper.Map<Staff>(request);
        newStaff.CreatedAt = VnClock.Now;
        newStaff.UpdatedAt = VnClock.Now;
        return newStaff;
    }
}
