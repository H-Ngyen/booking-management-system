using API.Common;
using API.Constraints;
using API.DTOs;
using API.Entities;
using API.Exceptions;
using API.Hubs;
using API.Interfaces;
using API.Interfaces.Authorization;
using API.Interfaces.Repositories;
using API.Interfaces.Services;
using AutoMapper;

namespace API.Services;

public class BookingsService(IBookingsRepository bookingsRepository,
    IServicesRepository servicesRepository,
    IStaffsRepository staffsRepository,
    IWorkSchedulesRepository workSchedulesRepository,
    IMapper mapper,
    IUserContext userContext,
    IUserRepository userRepository,
    IBookingsAuthorization bookingsAuthorization,
    IBookingNotifier notifier) : IBookingsService
{
    private const int SlotStepMinutes = 15;

    public async Task<PagedResult<BookingDto>> GetMyBookings(GetBookingsRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User me = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();
        // Read-own-list needs no resource: ownership is inherent in the filter.
        // Only the active check applies (Authorize+Read would demand a resource).
        if(!me.IsActive)
            throw new ForbidException();

        (IEnumerable<Booking>? bookings, int totalCount) = await bookingsRepository.GetAllMatchAsync(
            request.Date,
            request.Status,
            currentUser.Id,
            request.PageSize,
            request.PageNumber);

        var bookingDtos = mapper.Map<IEnumerable<BookingDto>>(bookings);
        var result = new PagedResult<BookingDto>(bookingDtos, totalCount, request.PageSize, request.PageNumber);

        return result;
    }

    public async Task<PagedResult<BookingDto>> GetAllBookings(GetBookingsRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        if(currentUser.Role != nameof(UserRole.Admin))
            throw new ForbidException();

        (IEnumerable<Booking>? bookings, int totalCount) = await bookingsRepository.GetAllMatchAsync(
            request.Date,
            request.Status,
            null,
            request.PageSize,
            request.PageNumber);

        var bookingDtos = mapper.Map<IEnumerable<BookingDto>>(bookings);
        var result = new PagedResult<BookingDto>(bookingDtos, totalCount, request.PageSize, request.PageNumber);

        return result;
    }

    public async Task<IEnumerable<string>> GetAvailableSlots(GetAvailableSlotsRequest request)
    {
        Service service = await servicesRepository.GetById(request.ServiceId) ?? throw new NotFoundException("Không tìm thấy dịch vụ.", "SERVICE_NOT_FOUND");
        Staff staff = await staffsRepository.GetById(request.StaffId) ?? throw new NotFoundException("Không tìm thấy nhân viên.", "STAFF_NOT_FOUND");
        if(!service.IsActive || !staff.IsActive)
            return [];

        DateOnly date = request.Date!.Value;
        IEnumerable<WorkSchedule> shifts = await workSchedulesRepository.GetSchedulesAsync(request.StaffId, date, date);
        IEnumerable<Booking> activeBookings = await bookingsRepository.GetActiveBookingsAsync(request.StaffId, date);

        var now = VnClock.Now;
        var slots = new List<string>();
        foreach(WorkSchedule shift in shifts)
        {
            DateTime shiftStart = shift.WorkDate.ToDateTime(shift.StartTime);
            DateTime shiftEnd = shift.WorkDate.ToDateTime(shift.EndTime);
            for(DateTime cursor = shiftStart; cursor.AddMinutes(service.DurationMinutes) <= shiftEnd; cursor = cursor.AddMinutes(SlotStepMinutes))
            {
                DateTime slotEnd = cursor.AddMinutes(service.DurationMinutes);
                if(cursor <= now)
                    continue;
                bool clash = activeBookings.Any(b => cursor < b.EndTime && slotEnd > b.StartTime);
                if(!clash)
                    slots.Add(cursor.ToString("o"));
            }
        }

        return slots;
    }

    public async Task<BookingDto> Create(CreateBookingRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User user = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();

        if(!bookingsAuthorization.Authorize(user, ResourceOperation.Create))
            throw new ForbidException();

        Service service = await servicesRepository.GetById(request.ServiceId) ?? throw new NotFoundException("Không tìm thấy dịch vụ.", "SERVICE_NOT_FOUND");
        if(!service.IsActive)
            throw new BadRequestException("Dịch vụ này hiện đang tạm khóa.", "SERVICE_INACTIVE");

        Staff staff = await staffsRepository.GetById(request.StaffId) ?? throw new NotFoundException("Không tìm thấy nhân viên.", "STAFF_NOT_FOUND");
        if(!staff.IsActive)
            throw new BadRequestException("Nhân viên này hiện đang tạm nghỉ.", "STAFF_INACTIVE");

        // Frontend sends VN wall time (no offset). Force Unspecified kind so the
        // value is stored/compared as wall time, never shifted.
        DateTime start = DateTime.SpecifyKind(request.StartTime!.Value, DateTimeKind.Unspecified);
        if(start <= VnClock.Now)
            throw new BadRequestException("Không thể đặt lịch trong quá khứ.", "BOOKING_IN_PAST");

        DateTime end = start.AddMinutes(service.DurationMinutes);
        if(!await IsWithinWorkingHours(request.StaffId, start, end))
            throw new BadRequestException("Khung giờ nằm ngoài giờ làm việc của nhân viên.", "OUTSIDE_WORKING_HOURS");

        DateTime now = VnClock.Now;
        Booking newBooking = new()
        {
            BookingCode = await GenerateBookingCodeAsync(start),
            CustomerId = user.Id,
            ServiceId = service.Id,
            StaffId = staff.Id,
            StartTime = start,
            EndTime = end,
            Status = BookingStatus.Pending,
            CustomerNote = request.CustomerNote,
            CreatedAt = now,
            UpdatedAt = now,
        };

        newBooking = await bookingsRepository.CreateBookingAsync(newBooking)
            ?? throw new ConflictException("Khung giờ này vừa có người đặt. Vui lòng chọn khung giờ khác.", "BOOKING_CONFLICT");

        await notifier.NotifyBookingChangedAsync(newBooking.Id, newBooking.CustomerId, BookingChangeTypes.Created);

        // Freshly inserted entity carries no navigations: fill display names
        // from the already-loaded service/user instead of an extra query.
        var created = mapper.Map<BookingDto>(newBooking);
        created.ServiceName = service.Name;
        created.CustomerName = user.UserName;
        created.StaffName = staff.FullName;
        return created;
    }

    private async Task<string> GenerateBookingCodeAsync(DateTime start)
    {
        const string chars32 = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        for(int attempt = 0; attempt < 5; attempt++)
        {
            string suffix = new(Enumerable.Range(0, 6).Select(_ => chars32[Random.Shared.Next(chars32.Length)]).ToArray());
            string code = $"BK-{start:yyyyMMdd}-{suffix}";
            if(!await bookingsRepository.ExistsByCodeAsync(code))
                return code;
        }

        throw new BadRequestException("Không thể tạo mã booking, vui lòng thử lại.", "BOOKING_CODE_FAILED");
    }

    public async Task<BookingDto> UpdateStatus(int id, UpdateBookingStatusRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User user = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();

        Booking booking = await bookingsRepository.GetById(id) ?? throw new NotFoundException("Không tìm thấy booking.", "BOOKING_NOT_FOUND");

        if(!bookingsAuthorization.Authorize(user, ResourceOperation.Update, booking))
            throw new ForbidException();

        BookingStatus target = request.Status!.Value;
        bool allowed = (booking.Status, target) switch
        {
            (BookingStatus.Pending, BookingStatus.Confirmed) => true,
            (BookingStatus.Confirmed, BookingStatus.Completed) => true,
            _ => false,
        };
        if(!allowed)
            throw new BadRequestException($"Không thể chuyển từ {booking.Status} sang {target}.", "STATUS_TRANSITION");

        booking.Status = target;
        booking.UpdatedAt = VnClock.Now;

        await bookingsRepository.SaveChanges();

        await notifier.NotifyBookingChangedAsync(booking.Id, booking.CustomerId, BookingChangeTypes.StatusChanged);

        var result = mapper.Map<BookingDto>(booking);
        return result;
    }

    public async Task<BookingDto> CancelBooking(int id, CancelBookingRequest request)
    {
        CurrentUser currentUser = userContext.GetCurrentUser();
        User user = await userRepository.GetUserById(currentUser.Id) ?? throw new ForbidException();

        Booking booking = await bookingsRepository.GetById(id) ?? throw new NotFoundException("Không tìm thấy booking.", "BOOKING_NOT_FOUND");

        if(!bookingsAuthorization.Authorize(user, ResourceOperation.Delete, booking))
            throw new ForbidException();

        if(booking.Status == BookingStatus.Cancelled)
            throw new BadRequestException("Booking này đã bị hủy trước đó.", "ALREADY_CANCELLED");
        if(booking.Status == BookingStatus.Completed || booking.StartTime <= VnClock.Now)
            throw new BadRequestException("Không thể hủy booking đã hoàn thành hoặc đã bắt đầu.", "CANNOT_CANCEL_STARTED");

        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = request.Reason;
        booking.UpdatedAt = VnClock.Now;

        await bookingsRepository.SaveChanges();

        await notifier.NotifyBookingChangedAsync(booking.Id, booking.CustomerId, BookingChangeTypes.Cancelled);

        return mapper.Map<BookingDto>(booking);
    }

    private async Task<bool> IsWithinWorkingHours(int staffId, DateTime start, DateTime end)
    {
        DateOnly date = DateOnly.FromDateTime(start);
        if(DateOnly.FromDateTime(end) != date)
            return false;

        IEnumerable<WorkSchedule> shifts = await workSchedulesRepository.GetSchedulesAsync(staffId, date, date);
        return shifts.Any(s =>
            s.WorkDate.ToDateTime(s.StartTime) <= start &&
            end <= s.WorkDate.ToDateTime(s.EndTime));
    }
}
