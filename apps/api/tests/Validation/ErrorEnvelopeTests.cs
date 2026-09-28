using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Tests.Support;

namespace Api.Tests.Validation;

// Contract: every backend error is JSON { message, code } (never plain text),
// so the frontend can always show a friendly message. Covers each status class.
public class ErrorEnvelopeTests(DbFixture fx) : ApiTestBase(fx)
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

    private static async Task<(string? Message, string? Code)> ReadEnvelope(HttpResponseMessage res)
    {
        using var doc = await res.Content.ReadFromJsonAsync<JsonDocument>(Json);
        var root = doc!.RootElement;
        return (
            root.TryGetProperty("message", out var m) ? m.GetString() : null,
            root.TryGetProperty("code", out var c) ? c.GetString() : null);
    }

    [Fact]
    public async Task Envelope_400ServiceInactive_CarriesCode()
    {
        var (admin, customer) = await ClientsAsync();
        var svcId = await (await admin.PostAsJsonAsync("/api/v1/services",
            new { name = "Svc", durationMinutes = 30, price = 1 })).Content.ReadFromJsonAsync<int>(Json);
        var staffId = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "St", email = $"{Guid.NewGuid():N}@t.local" })).Content.ReadFromJsonAsync<int>(Json);
        await admin.PutAsJsonAsync($"/api/v1/services/{svcId}",
            new { name = "Svc", durationMinutes = 30, price = 1, isActive = false });
        var date = TomorrowPlus(3).ToString("yyyy-MM-dd");
        await admin.PostAsJsonAsync($"/api/v1/staffs/{staffId}/schedules",
            new { workDate = date, startTime = "08:00", endTime = "18:00" });

        using var res = await customer.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = svcId, staffId, startTime = $"{date}T09:00:00" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var (message, code) = await ReadEnvelope(res);
        Assert.Equal("SERVICE_INACTIVE", code);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public async Task Envelope_401BadLogin_CarriesCode()
    {
        await Fx.ResetAsync();
        await SeedUsersAsync();
        using var res = await AnonClient().PostAsJsonAsync("/api/v1/auth/login",
            new { userName = "admin", password = "nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var (message, code) = await ReadEnvelope(res);
        Assert.Equal("INVALID_CREDENTIALS", code);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public async Task Envelope_403CustomerCreateService_CarriesCode()
    {
        var (_, customer) = await ClientsAsync();
        using var res = await customer.PostAsJsonAsync("/api/v1/services",
            new { name = "X", durationMinutes = 30, price = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        var (_, code) = await ReadEnvelope(res);
        Assert.Equal("FORBIDDEN", code);
    }

    [Fact]
    public async Task Envelope_404MissingStaff_CarriesCode()
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.GetAsync("/api/v1/staffs/999999/schedules");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var (_, code) = await ReadEnvelope(res);
        Assert.Equal("STAFF_NOT_FOUND", code);
    }

    [Fact]
    public async Task Envelope_409DuplicateStaffEmail_CarriesCode()
    {
        var (admin, _) = await ClientsAsync();
        await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "Dup", email = "dup@test.local" });
        using var res = await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "Dup2", email = "dup@test.local" });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var (_, code) = await ReadEnvelope(res);
        Assert.Equal("STAFF_EMAIL_EXISTS", code);
    }

    [Fact]
    public async Task Envelope_500PaginationOverflow_CarriesCode()
    {
        // Fixed F-500-01 (offset clamped via long math): huge page numbers now
        // return 200 with an empty page and sane counters instead of 500.
        var (admin, _) = await ClientsAsync();
        using var res = await admin.GetAsync("/api/v1/services?pageNumber=2147483647&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        using var doc = await res.Content.ReadFromJsonAsync<JsonDocument>(Json);
        var root = doc!.RootElement;
        Assert.Equal(0, root.GetProperty("totalItemsCount").GetInt32());
        Assert.True(root.GetProperty("itemsFrom").GetInt32() >= 0);
        Assert.True(root.GetProperty("itemsTo").GetInt32() >= 0);
    }
}
