using API.Constraints;

namespace API.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
}

public record CurrentUser(int Id, string Role) { }