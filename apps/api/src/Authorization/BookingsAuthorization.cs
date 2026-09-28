using API.Constraints;
using API.Entities;
using API.Interfaces.Authorization;

namespace API.Authorization;

public class BookingsAuthorization : IBookingsAuthorization
{
    public bool Authorize(User user, ResourceOperation operation, Booking? resource)
    {
        if(!user.IsActive) return false;

        // Admin: "I'm god!!!"
        if(user.Role == UserRole.Admin) return true;

        if(operation == ResourceOperation.Create) return true;
        if((operation == ResourceOperation.Read || operation == ResourceOperation.Delete) 
            && resource != null && resource.CustomerId == user.Id)
            return true;
        return false;
    }

}
