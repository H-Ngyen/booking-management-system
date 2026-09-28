using System.Net;
using System.Net.Http.Json;
using API.DTOs;
using Api.Tests.Support;

namespace Api.Tests.Staffs;

// §3 Staffs + work schedules.
public class StaffScheduleTests(DbFixture fx) : ApiTestBase(fx)
{
    private async Task<(HttpClient Admin, HttpClient Customer)> ClientsAsync()
    {
        await Fx.ResetAsync();
        await SeedUsersAsync();
        using var anon = AnonClient();
        return (
            AuthedClient(await LoginAsync(anon, "admin", "pw123")),
            AuthedClient(await LoginAsync(anon, "customer1", "pw123")));
    }

    [Fact]
    public async Task Admin_CreateStaff_Returns201ThenDuplicateEmail409()
    {
        var (admin, _) = await ClientsAsync();
        using var created = await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "New Staff", email = "new.staff@test.local" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var dup = await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "Dup", email = "new.staff@test.local" });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Theory]
    [InlineData("", "a@test.local")]       // missing name
    [InlineData("Ok", "not-an-email")]     // invalid email
    [InlineData("Ok", "")]                 // missing email
    public async Task CreateStaff_InvalidInput_Returns400(string name, string email)
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = name, email });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Customer_CreateStaff_Returns403()
    {
        var (_, customer) = await ClientsAsync();
        using var res = await customer.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "X", email = "x@test.local" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Customer_CanReadStaffList_ButLockedHidden()
    {
        var (admin, customer) = await ClientsAsync();
        var id = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "ToLock", email = "tolock@test.local" })).Content.ReadFromJsonAsync<int>(Json);
        using var lockRes = await admin.PutAsJsonAsync($"/api/v1/staffs/{id}",
            new { fullName = "ToLock", email = "tolock@test.local", isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, lockRes.StatusCode);

        var cust = await ReadPage<StaffDto>(await customer.GetAsync("/api/v1/staffs?pageNumber=1&pageSize=50"));
        Assert.DoesNotContain(cust.Items, s => s.Id == id);
    }

    [Fact]
    public async Task UpdateStaff_Nonexistent_Returns404()
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.PutAsJsonAsync("/api/v1/staffs/999999",
            new { fullName = "X", email = "x@test.local", isActive = true });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task CreateSchedule_ThenOverlappingShift_Returns409()
    {
        var (admin, _) = await ClientsAsync();
        var staffId = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "S", email = "s@test.local" })).Content.ReadFromJsonAsync<int>(Json);
        var date = TomorrowPlus(1).ToString("yyyy-MM-dd");

        using var first = await admin.PostAsJsonAsync($"/api/v1/staffs/{staffId}/schedules",
            new { workDate = date, startTime = "08:00", endTime = "12:00" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var overlap = await admin.PostAsJsonAsync($"/api/v1/staffs/{staffId}/schedules",
            new { workDate = date, startTime = "10:00", endTime = "14:00" });
        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);

        // Boundary (end == start) is NOT an overlap: must succeed.
        using var adjacent = await admin.PostAsJsonAsync($"/api/v1/staffs/{staffId}/schedules",
            new { workDate = date, startTime = "12:00", endTime = "14:00" });
        Assert.Equal(HttpStatusCode.Created, adjacent.StatusCode);
    }

    [Fact]
    public async Task CreateSchedule_StartAfterEnd_Returns400()
    {
        var (admin, _) = await ClientsAsync();
        var staffId = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "S", email = "s@test.local" })).Content.ReadFromJsonAsync<int>(Json);
        var date = TomorrowPlus(1).ToString("yyyy-MM-dd");

        using var res = await admin.PostAsJsonAsync($"/api/v1/staffs/{staffId}/schedules",
            new { workDate = date, startTime = "14:00", endTime = "12:00" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreateSchedule_NonexistentStaff_Returns404()
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.PostAsJsonAsync("/api/v1/staffs/999999/schedules",
            new { workDate = TomorrowPlus(1).ToString("yyyy-MM-dd"), startTime = "08:00", endTime = "12:00" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Customer_CreateSchedule_Returns403()
    {
        var (admin, customer) = await ClientsAsync();
        var staffId = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "S", email = "s@test.local" })).Content.ReadFromJsonAsync<int>(Json);
        using var res = await customer.PostAsJsonAsync($"/api/v1/staffs/{staffId}/schedules",
            new { workDate = TomorrowPlus(1).ToString("yyyy-MM-dd"), startTime = "08:00", endTime = "12:00" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task DeleteSchedule_WithActiveBooking_Returns409_ThenSucceedsAfterCancel()
    {
        var (admin, _) = await ClientsAsync();
        using var anon = AnonClient();
        var custToken = await LoginAsync(anon, "customer1", "pw123");
        using var customer = AuthedClient(custToken);

        var staffId = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "S", email = "s@test.local" })).Content.ReadFromJsonAsync<int>(Json);
        var svcId = await (await admin.PostAsJsonAsync("/api/v1/services",
            new { name = "Svc", durationMinutes = 30, price = 1 })).Content.ReadFromJsonAsync<int>(Json);
        var date = TomorrowPlus(2).ToString("yyyy-MM-dd");
        var sch = await (await admin.PostAsJsonAsync($"/api/v1/staffs/{staffId}/schedules",
            new { workDate = date, startTime = "08:00", endTime = "12:00" })).Content
            .ReadFromJsonAsync<WorkScheduleDto>(Json);

        var booking = await (await customer.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = svcId, staffId, startTime = $"{date}T08:00:00" })).Content
            .ReadFromJsonAsync<BookingDto>(Json);

        using var blocked = await admin.DeleteAsync($"/api/v1/staffs/{staffId}/schedules/{sch!.Id}");
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

        using var cancel = await customer.PostAsJsonAsync($"/api/v1/bookings/{booking!.Id}/cancel",
            new { reason = "customer changed mind" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        using var deleted = await admin.DeleteAsync($"/api/v1/staffs/{staffId}/schedules/{sch.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task DeleteSchedule_NonexistentOrWrongStaff_Returns404()
    {
        var (admin, _) = await ClientsAsync();
        using var missing = await admin.DeleteAsync("/api/v1/staffs/999999/schedules/1");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var staffId = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "S", email = "s@test.local" })).Content.ReadFromJsonAsync<int>(Json);
        using var wrong = await admin.DeleteAsync($"/api/v1/staffs/{staffId}/schedules/999999");
        Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
    }

    [Fact]
    public async Task Customer_CanReadSchedules()
    {
        var (admin, customer) = await ClientsAsync();
        var staffId = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "S", email = "s@test.local" })).Content.ReadFromJsonAsync<int>(Json);
        var date = TomorrowPlus(1).ToString("yyyy-MM-dd");
        await admin.PostAsJsonAsync($"/api/v1/staffs/{staffId}/schedules",
            new { workDate = date, startTime = "08:00", endTime = "12:00" });

        using var res = await customer.GetAsync($"/api/v1/staffs/{staffId}/schedules");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var list = await res.Content.ReadFromJsonAsync<List<WorkScheduleDto>>(Json);
        Assert.Single(list!);
        Assert.Equal("08:00", list![0].StartTime);
    }
}
