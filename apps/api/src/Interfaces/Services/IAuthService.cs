using API.DTOs;

namespace API.Interfaces.Services;

public interface IAuthService
{
    Task<string> Login(LoginRequest request);
    Task<UserDto> Me(); 
}