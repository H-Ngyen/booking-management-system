using API.Constraints;
using API.Entities;
using API.Interfaces.Authorization;

namespace API.Authorization;

public class ServicesAuthorization : IServicesAuthorization
{
    public bool Authorize(User user, ResourceOperation operation, Service? resource)
    {
        if(!user.IsActive) return false;

        // Admin: "I'm god!!!"
        if(user.Role == UserRole.Admin) return true;
        if(operation == ResourceOperation.Read && user.Role == UserRole.Customer) return true;

        return false;
    }

}