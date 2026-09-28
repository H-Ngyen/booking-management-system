using API.Common;
using API.DTOs;
using API.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// api/v1/bookings
public class BookingsController(IBookingsService bookingsService) : BaseApiController
{
    [HttpGet("my-bookings")]
    public async Task<ActionResult<PagedResult<BookingDto>>> GetMyBookings([FromQuery] GetBookingsRequest request)
    {
        return await bookingsService.GetMyBookings(request);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BookingDto>>> GetAllBookings([FromQuery] GetBookingsRequest request)
    {
        return await bookingsService.GetAllBookings(request);
    }

    [HttpGet("available-slots")]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableSlots([FromQuery] GetAvailableSlotsRequest request)
    {
        return Ok(await bookingsService.GetAvailableSlots(request));
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingRequest request)
    {
        BookingDto booking = await bookingsService.Create(request);
        return Created("", booking);
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<BookingDto>> UpdateStatus(int id, UpdateBookingStatusRequest request)
    {
        return await bookingsService.UpdateStatus(id, request);
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<BookingDto>> CancelBooking(int id, CancelBookingRequest request)
    {
        return await bookingsService.CancelBooking(id, request);
    }
}
