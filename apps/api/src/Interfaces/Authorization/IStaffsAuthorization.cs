using API.Constraints;
using API.Entities;

namespace API.Interfaces.Authorization;

public interface IStaffsAuthorization
{
    bool Authorize(User user, ResourceOperation operation, Staff? resource = null);
}
