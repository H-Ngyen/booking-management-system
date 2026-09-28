namespace API.Entities
{
    public class WorkSchedule
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public DateOnly WorkDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Staff Staff { get; set; } = null!;
    }
}
