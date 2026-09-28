using API.DTOs;

namespace API.Interfaces;

public interface IUserContext
{
    CurrentUser GetCurrentUser();
}