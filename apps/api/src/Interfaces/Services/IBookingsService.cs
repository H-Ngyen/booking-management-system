using API.Common;
using API.Constraints;
using API.DTOs;

namespace API.Interfaces.Services;

public interface IBookingsService
{
    Task<PagedResult<BookingDto>> GetMyBookings(GetBookingsRequest request);
    Task<PagedResult<BookingDto>> GetAllBookings(GetBookingsRequest request);
    Task<IEnumerable<string>> GetAvailableSlots(GetAvailableSlotsRequest request);
    Task<BookingDto> Create(CreateBookingRequest request);
    Task<BookingDto> UpdateStatus(int id, UpdateBookingStatusRequest request);
    Task<BookingDto> CancelBooking(int id, CancelBookingRequest request);
}
