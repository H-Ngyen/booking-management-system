using API.Constraints;

namespace API.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer;
        public required string PasswordHash { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<Booking> Bookings { get; set; } = [];
    }
}
