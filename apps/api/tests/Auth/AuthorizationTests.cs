using API.Authorization;
using API.Constraints;
using API.Entities;

namespace Api.Tests.Auth;

// Pure unit tests (no DB, no HTTP): authorization matrices are pure logic,
// so they are tested in isolation — no mocks needed, no external systems.
public class AuthorizationTests
{
    private static User Account(UserRole role, int id = 7, bool active = true) =>
        new() { Id = id, UserName = "u", Email = "u@t.local", Role = role, PasswordHash = "x", IsActive = active };

    private static Booking Owned(int ownerId) =>
        new() { Id = 1, BookingCode = "BK-T", CustomerId = ownerId, Status = BookingStatus.Pending };

    [Theory]
    [InlineData(ResourceOperation.Create)]
    [InlineData(ResourceOperation.Read)]
    [InlineData(ResourceOperation.Update)]
    [InlineData(ResourceOperation.Delete)]
    public void Services_Admin_IsGod(ResourceOperation op) =>
        Assert.True(new ServicesAuthorization().Authorize(Account(UserRole.Admin), op, null));

    [Fact]
    public void Services_Customer_CanOnlyRead() =>
        Assert.True(new ServicesAuthorization().Authorize(Account(UserRole.Customer), ResourceOperation.Read, null));

    [Theory]
    [InlineData(ResourceOperation.Create)]
    [InlineData(ResourceOperation.Update)]
    [InlineData(ResourceOperation.Delete)]
    public void Services_Customer_CannotWrite(ResourceOperation op) =>
        Assert.False(new ServicesAuthorization().Authorize(Account(UserRole.Customer), op, null));

    [Fact]
    public void Services_InactiveAdmin_Denied() =>
        Assert.False(new ServicesAuthorization().Authorize(Account(UserRole.Admin, active: false), ResourceOperation.Read, null));

    [Fact]
    public void Bookings_Customer_CanCreate() =>
        Assert.True(new BookingsAuthorization().Authorize(Account(UserRole.Customer), ResourceOperation.Create, null));

    [Fact]
    public void Bookings_Customer_CanReadAndCancelOwn()
    {
        var auth = new BookingsAuthorization();
        Assert.True(auth.Authorize(Account(UserRole.Customer, id: 7), ResourceOperation.Read, Owned(7)));
        Assert.True(auth.Authorize(Account(UserRole.Customer, id: 7), ResourceOperation.Delete, Owned(7)));
    }

    [Fact]
    public void Bookings_Customer_CannotTouchOthers()
    {
        var auth = new BookingsAuthorization();
        Assert.False(auth.Authorize(Account(UserRole.Customer, id: 7), ResourceOperation.Read, Owned(9)));
        Assert.False(auth.Authorize(Account(UserRole.Customer, id: 7), ResourceOperation.Delete, Owned(9)));
    }

    [Theory]
    [InlineData(ResourceOperation.Update)]
    public void Bookings_Customer_CannotSelfConfirmOrComplete(ResourceOperation op) =>
        Assert.False(new BookingsAuthorization().Authorize(Account(UserRole.Customer, id: 7), op, Owned(7)));

    [Fact]
    public void Staffs_Customer_CanRead_NotWrite()
    {
        var auth = new StaffsAuthorization();
        Assert.True(auth.Authorize(Account(UserRole.Customer), ResourceOperation.Read, null));
        Assert.False(auth.Authorize(Account(UserRole.Customer), ResourceOperation.Create, null));
        Assert.False(auth.Authorize(Account(UserRole.Customer), ResourceOperation.Delete, null));
    }

    [Fact]
    public void Staffs_InactiveUser_DeniedEverything()
    {
        var auth = new StaffsAuthorization();
        var inactive = Account(UserRole.Customer, active: false);
        foreach (var op in Enum.GetValues<ResourceOperation>())
            Assert.False(auth.Authorize(inactive, op, null));
    }
}
