using API.DTOs;

namespace API.Interfaces.Services;

public interface ITokenService
{
    public string CreateToken(CurrentUser user);
}