using System.ComponentModel.DataAnnotations;
using API.Constraints;

namespace API.DTOs;

//Request
public class GetBookingsRequest
{
    public DateOnly? Date { get; set; }
    public BookingStatus? Status { get; set; }

    [Range(1, 100, ErrorMessage = "PageSize phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 20;

    [Range(1, int.MaxValue, ErrorMessage = "PageNumber phải lớn hơn 0.")]
    public int PageNumber { get; set; } = 1;
}

public class CreateBookingRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "ServiceId không hợp lệ.")]
    public int ServiceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "StaffId không hợp lệ.")]
    public int StaffId { get; set; }

    [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc (ISO 8601).")]
    public DateTime? StartTime { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự.")]
    public string? CustomerNote { get; set; }
}

public class UpdateBookingStatusRequest
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
    [EnumDataType(typeof(BookingStatus), ErrorMessage = "Trạng thái không hợp lệ.")]
    public BookingStatus? Status { get; set; }
}

public class CancelBookingRequest
{
    [Required(ErrorMessage = "Lý do hủy là bắt buộc.")]
    [StringLength(1000, ErrorMessage = "Lý do hủy tối đa 1000 ký tự.")]
    public required string Reason { get; set; }
}

public class GetAvailableSlotsRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "ServiceId không hợp lệ.")]
    public int ServiceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "StaffId không hợp lệ.")]
    public int StaffId { get; set; }

    [Required(ErrorMessage = "Ngày là bắt buộc (YYYY-MM-DD).")]
    public DateOnly? Date { get; set; }
}

// Response
public class BookingDto
{
    public int Id { get; set; }
    public required string BookingCode { get; set; }
    public int CustomerId { get; set; }
    public int ServiceId { get; set; }
    public int StaffId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingStatus Status { get; set; }
    public string? CustomerNote { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
