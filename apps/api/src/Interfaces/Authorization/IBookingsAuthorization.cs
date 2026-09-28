using API.Constraints;
using API.Entities;

namespace API.Interfaces.Authorization;

public interface IBookingsAuthorization
{
    bool Authorize(User user, ResourceOperation operation, Booking? resource = null);
}
