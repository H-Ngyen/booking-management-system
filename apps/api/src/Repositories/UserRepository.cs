using API.Data;
using API.Entities;
using API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class UserRepository(DataContext context) : BaseRepository<User>(context), IUserRepository
{
    public async Task<bool> ExistByUserNameAsync(string userName)
        => await NoTrackingQuery.AnyAsync(u => u.UserName == userName);

    public async Task<User?> GetUserById(int id)
    {
        return await TrackingQuery.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetUserByUserName(string userName)
    {
        return await TrackingQuery.FirstOrDefaultAsync(u => u.UserName == userName);
    }
}