using API.Constraints;

namespace API.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public required string BookingCode { get; set; }
        public int CustomerId { get; set; }
        public int ServiceId { get; set; }
        public int StaffId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public string? CustomerNote { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User Customer { get; set; } = null!;
        public Service Service { get; set; } = null!;
        public Staff Staff { get; set; } = null!;
    }
}
