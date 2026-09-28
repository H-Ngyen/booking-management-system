using System.Net;
using System.Net.Http.Json;
using System.Text;
using Api.Tests.Support;

namespace Api.Tests.Validation;

// White-box adversarial suite: inputs engineered from reading the controllers,
// DTO validation attributes and repository code, aiming to surface HTTP 500s
// (unhandled paths) and validation holes. Failing tests here document APP bugs
// (see docs/TestReport.md) and are intentionally left red — app code is NOT
// modified to make them pass.
public class AdversarialTests(DbFixture fx) : ApiTestBase(fx)
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

    // Booking creation context shared by destructive-input tests.
    private async Task<(HttpClient Admin, HttpClient Customer, int Svc, int Staff, string Date)> BookingCtxAsync()
    {
        var (admin, customer) = await ClientsAsync();
        var svc = await (await admin.PostAsJsonAsync("/api/v1/services",
            new { name = "Svc", durationMinutes = 60, price = 1 })).Content.ReadFromJsonAsync<int>(Json);
        var staff = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "St", email = $"{Guid.NewGuid():N}@t.local" })).Content.ReadFromJsonAsync<int>(Json);
        var date = TomorrowPlus(5).ToString("yyyy-MM-dd");
        using var sch = await admin.PostAsJsonAsync($"/api/v1/staffs/{staff}/schedules",
            new { workDate = date, startTime = "08:00", endTime = "18:00" });
        sch.EnsureSuccessStatusCode();
        return (admin, customer, svc, staff, date);
    }

    [Fact]
    public async Task ADV_PaginationOverflow_PageNumberMax_No500()
    {
        // White-box: Skip(pageSize * (pageNumber-1)) overflows int32 for huge
        // pageNumber; Skip(negative) throws ArgumentOutOfRangeException.
        var (admin, _) = await ClientsAsync();
        foreach (var path in new[]
        {
            $"/api/v1/bookings?pageNumber={int.MaxValue}&pageSize=20",
            $"/api/v1/services?pageNumber={int.MaxValue}&pageSize=20",
            $"/api/v1/staffs?pageNumber={int.MaxValue}&pageSize=20",
        })
        {
            using var res = await admin.GetAsync(path);
            Assert.True(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
                $"{path} returned {(int)res.StatusCode} (expected 200/400, never 500)");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ADV_PaginationBounds_Return400(int pageSize)
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.GetAsync($"/api/v1/bookings?pageNumber=1&pageSize={pageSize}");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ADV_BookingStatusQuery_InvalidValue_No500()
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.GetAsync("/api/v1/bookings?status=BOGUS&pageNumber=1&pageSize=5");
        Assert.True(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"status=BOGUS returned {(int)res.StatusCode}, never 500");
    }

    [Fact]
    public async Task ADV_BookingStatusQuery_Lowercase_IsAccepted()
    {
        var (admin, customer) = await ClientsAsync();
        var (_, _, svc, staff, date) = await BookingCtxAsync();
        using var c = await customer.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = svc, staffId = staff, startTime = $"{date}T09:00:00" });
        c.EnsureSuccessStatusCode();
        using var res = await admin.GetAsync($"/api/v1/bookings?status=pending&pageNumber=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task ADV_BookingDateQuery_Garbage_No500()
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.GetAsync("/api/v1/bookings?date=not-a-date&pageNumber=1&pageSize=5");
        Assert.True(res.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"garbage date returned {(int)res.StatusCode}, never 500");
    }

    [Fact]
    public async Task ADV_PatchStatus_MissingBody_Returns400()
    {
        var (_, customer) = await ClientsAsync();
        using var res = await customer.PatchAsJsonAsync("/api/v1/bookings/1/status", new { });
        Assert.True(res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound,
            $"empty status body returned {(int)res.StatusCode}");
    }

    [Fact]
    public async Task ADV_PatchStatus_IntegerOutOfRange_No500()
    {
        // STJ materializes unknown int enums without error; EnumDataType must reject.
        var (admin, customer) = await ClientsAsync();
        var (_, _, svc, staff, date) = await BookingCtxAsync();
        var created = await (await customer.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = svc, staffId = staff, startTime = $"{date}T09:00:00" })).Content
            .ReadAsStringAsync();
        var id = System.Text.Json.JsonDocument.Parse(created).RootElement.GetProperty("id").GetInt32();
        using var raw = new StringContent("{\"status\":99}", Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/bookings/{id}/status") { Content = raw };
        using var res = await admin.SendAsync(req);
        Assert.True(res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound,
            $"status=99 returned {(int)res.StatusCode}, never 500");
    }

    [Fact]
    public async Task ADV_PatchStatus_LowercaseString_No500()
    {
        var (admin, customer) = await ClientsAsync();
        var (_, _, svc, staff, date) = await BookingCtxAsync();
        var created = await (await customer.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = svc, staffId = staff, startTime = $"{date}T09:00:00" })).Content
            .ReadAsStringAsync();
        var id = System.Text.Json.JsonDocument.Parse(created).RootElement.GetProperty("id").GetInt32();
        using var raw = new StringContent("{\"status\":\"confirmed\"}", Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/bookings/{id}/status") { Content = raw };
        using var res = await admin.SendAsync(req);
        Assert.True(
            res.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.OK,
            $"lowercase enum returned {(int)res.StatusCode}, never 500");
    }

    [Fact]
    public async Task ADV_CreateBooking_EmptyObject_Returns400()
    {
        var (_, customer) = await ClientsAsync();
        using var res = await customer.PostAsJsonAsync("/api/v1/bookings", new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ADV_CreateBooking_UnparsableStartTime_Returns400()
    {
        var (_, customer) = await ClientsAsync();
        using var raw = new StringContent(
            "{\"serviceId\":1,\"staffId\":1,\"startTime\":\"not-a-date\"}", Encoding.UTF8, "application/json");
        using var res = await customer.PostAsync("/api/v1/bookings", raw);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ADV_CreateBooking_AncientDate_Returns400Not500()
    {
        var (_, customer) = await ClientsAsync();
        var (_, _, svc, staff, _) = await BookingCtxAsync();
        using var res = await customer.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = svc, staffId = staff, startTime = "0001-01-01T00:00:00" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ADV_CreateBooking_WallTime_StoredVerbatim_NoShift()
    {
        // Wall-time regime: what the client sends is what gets stored.
        // No UTC conversion happens anywhere in the pipeline.
        var (admin, customer) = await ClientsAsync();
        var (_, _, svc, staff, date) = await BookingCtxAsync();
        using var res = await customer.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = svc, staffId = staff, startTime = $"{date}T15:00:00" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        var doc = System.Text.Json.JsonDocument.Parse(body);
        var start = doc.RootElement.GetProperty("startTime").GetDateTime();
        var end = doc.RootElement.GetProperty("endTime").GetDateTime();
        Assert.Equal(15, start.Hour); // stored verbatim, not shifted
        Assert.Equal(DateTimeKind.Unspecified, start.Kind);
        Assert.Equal(TimeSpan.FromHours(1), end - start);
        _ = admin;
    }

    [Fact]
    public async Task ADV_CreateBooking_NoteTooLong_Returns400()
    {
        var (_, customer) = await ClientsAsync();
        var (_, _, svc, staff, date) = await BookingCtxAsync();
        using var res = await customer.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = svc, staffId = staff, startTime = $"{date}T09:00:00Z", customerNote = new string('x', 1001) });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ADV_Cancel_WhitespaceOnlyReason_RejectedWith400()
    {
        var (_, customer) = await ClientsAsync();
        var (_, _, svc, staff, date) = await BookingCtxAsync();
        var created = await (await customer.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = svc, staffId = staff, startTime = $"{date}T09:00:00" })).Content
            .ReadAsStringAsync();
        var id = System.Text.Json.JsonDocument.Parse(created).RootElement.GetProperty("id").GetInt32();
        // [Required] passes whitespace-only strings: must still be rejected.
        using var res = await customer.PostAsJsonAsync($"/api/v1/bookings/{id}/cancel", new { reason = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ADV_CreateStaff_WhitespaceOnlyName_RejectedWith400()
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "   ", email = "w@test.local" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ADV_ConcurrentCancelSameBooking_OneWins_No500()
    {
        var (_, customer) = await ClientsAsync();
        var (_, _, svc, staff, date) = await BookingCtxAsync();
        var created = await (await customer.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = svc, staffId = staff, startTime = $"{date}T09:00:00" })).Content
            .ReadAsStringAsync();
        var id = System.Text.Json.JsonDocument.Parse(created).RootElement.GetProperty("id").GetInt32();

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            customer.PostAsJsonAsync($"/api/v1/bookings/{id}/cancel", new { reason = "race" })));
        // Deterministic invariants only: no 500, nothing lost, final state Cancelled.
        // The exact 200/400 split is timing-dependent (lost-update race F-RACE-01:
        // two readers can both see Pending and both succeed) — see TestReport.
        Assert.Equal(5, results.Length);
        Assert.All(results, r => Assert.True(
            r.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            $"unexpected {(int)r.StatusCode}"));
        Assert.Contains(results, r => r.StatusCode == HttpStatusCode.OK);
        foreach (var r in results) r.Dispose();
    }

    [Fact]
    public async Task ADV_UnknownRoute_Returns404()
    {
        await Fx.ResetAsync();
        using var res = await AnonClient().GetAsync("/api/v1/nope");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task ADV_WrongContentType_Returns415()
    {
        var (_, customer) = await ClientsAsync();
        using var content = new StringContent("{}", Encoding.UTF8, "text/plain");
        using var res = await customer.PostAsync("/api/v1/bookings", content);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, res.StatusCode);
    }

    [Fact]
    public async Task ADV_Schedule_FarPastDate_AcceptedOrRejected_Deterministically()
    {
        // No past-check exists on schedule creation (white-box observation):
        // assert the CURRENT contract (201) so any future change is detected.
        var (admin, _) = await ClientsAsync();
        var staffId = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "S", email = $"{Guid.NewGuid():N}@t.local" })).Content.ReadFromJsonAsync<int>(Json);
        using var res = await admin.PostAsJsonAsync($"/api/v1/staffs/{staffId}/schedules",
            new { workDate = "2020-05-05", startTime = "08:00", endTime = "12:00" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task ADV_Schedules_ReversedRange_Returns200Empty()
    {
        var (admin, _) = await ClientsAsync();
        var staffId = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "S", email = $"{Guid.NewGuid():N}@t.local" })).Content.ReadFromJsonAsync<int>(Json);
        using var res = await admin.GetAsync(
            $"/api/v1/staffs/{staffId}/schedules?from=2027-01-10&to=2027-01-01");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var list = await res.Content.ReadFromJsonAsync<List<API.DTOs.WorkScheduleDto>>(Json);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task ADV_StaffIdMismatch_OnDeleteSchedule_Returns404()
    {
        var (admin, _) = await ClientsAsync();
        var s1 = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "S1", email = $"{Guid.NewGuid():N}@t.local" })).Content.ReadFromJsonAsync<int>(Json);
        var s2 = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "S2", email = $"{Guid.NewGuid():N}@t.local" })).Content.ReadFromJsonAsync<int>(Json);
        var date = TomorrowPlus(6).ToString("yyyy-MM-dd");
        var sch = await (await admin.PostAsJsonAsync($"/api/v1/staffs/{s1}/schedules",
            new { workDate = date, startTime = "08:00", endTime = "12:00" })).Content
            .ReadFromJsonAsync<API.DTOs.WorkScheduleDto>(Json);
        using var res = await admin.DeleteAsync($"/api/v1/staffs/{s2}/schedules/{sch!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task ADV_Login_NullBody_Returns400Not500()
    {
        await Fx.ResetAsync();
        using var client = AnonClient();
        using var content = new StringContent("", Encoding.UTF8, "application/json");
        using var res = await client.PostAsync("/api/v1/auth/login", content);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ADV_UpdateService_PartialBody_Returns400()
    {
        // Backend update requires the FULL object (all fields validated).
        var (admin, _) = await ClientsAsync();
        var id = await (await admin.PostAsJsonAsync("/api/v1/services",
            new { name = "S", durationMinutes = 30, price = 1 })).Content.ReadFromJsonAsync<int>(Json);
        using var res = await admin.PutAsJsonAsync($"/api/v1/services/{id}", new { name = "Only-Name" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ADV_InactiveUser_CannotReadOwnBookings()
    {
        await Fx.ResetAsync();
        await SeedUsersAsync();
        var locked = await AddUserAsync("locked_reader", API.Constraints.UserRole.Customer, isActive: false);
        Assert.True(locked.Id > 0);
        using var anon = AnonClient();
        // Locked accounts get no token at login (fixed F-AUTH-01), so there is
        // nothing to read with: login itself must be 401.
        using var res = await anon.PostAsJsonAsync("/api/v1/auth/login",
            new { userName = "locked_reader", password = "pw123" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
