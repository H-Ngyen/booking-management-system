namespace API.Entities
{
    public class Staff
    {
        public int Id { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<WorkSchedule> Schedules { get; set; } = [];
        public ICollection<Booking> Bookings { get; set; } = [];
    }
}
