using API.Entities;

namespace API.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByUserName(string userName); 
    Task<bool> ExistByUserNameAsync(string userName);
    Task<User?> GetUserById(int id);
}