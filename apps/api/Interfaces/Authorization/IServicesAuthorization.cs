using API.Constraints;
using API.Entities;

namespace API.Interfaces.Authorization;

public interface IServicesAuthorization
{
    bool Authorize(User user, ResourceOperation operation, Service? resource = null);
}