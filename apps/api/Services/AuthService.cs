using API.Repositories;

namespace API.Serivces;

public interface IAuthService
{
    
}

public class AuthService (
    IAuthRepository authRepository,
    IJwtService jwt) : IAuthService
{
    
}