using System.Net;
using System.Net.Http.Json;
using Api.Tests.Support;

namespace Api.Tests.Auth;

// §1 + §7 Authentication. Every test resets the DB: no shared state.
public class AuthTests(DbFixture fx) : ApiTestBase(fx)
{
    [Fact]
    public async Task Login_ValidAdmin_Returns200WithToken()
    {
        await Fx.ResetAsync();
        await SeedUsersAsync();
        using var client = AnonClient();

        using var res = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { userName = "admin", password = "pw123" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await res.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Login_ValidCustomer_Returns200()
    {
        await Fx.ResetAsync();
        await SeedUsersAsync();
        using var client = AnonClient();
        var token = await LoginAsync(client, "customer1", "pw123");
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Theory]
    [InlineData("admin", "wrongpassword")]
    [InlineData("nosuchuser", "pw123")]
    public async Task Login_BadCredentials_Returns401(string userName, string password)
    {
        await Fx.ResetAsync();
        await SeedUsersAsync();
        using var client = AnonClient();

        using var res = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { userName, password });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_MissingFields_Returns400ValidationEnvelope()
    {
        await Fx.ResetAsync();
        using var client = AnonClient();

        using var res = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { userName = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("VALIDATION_ERROR", body);
    }

    [Fact]
    public async Task Login_InvalidUserNameFormat_Returns400()
    {
        await Fx.ResetAsync();
        using var client = AnonClient();

        using var res = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { userName = "has space!", password = "pw123" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        await Fx.ResetAsync();
        using var res = await AnonClient().GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsProfileWithoutPasswordHash()
    {
        await Fx.ResetAsync();
        await SeedUsersAsync();
        using var anon = AnonClient();
        using var client = AuthedClient(await LoginAsync(anon, "customer1", "pw123"));

        using var res = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("customer1", body);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pw123", body);
    }

    [Fact]
    public async Task Me_TamperedToken_Returns401()
    {
        await Fx.ResetAsync();
        await SeedUsersAsync();
        using var anon = AnonClient();
        var token = await LoginAsync(anon, "customer1", "pw123");
        using var client = AuthedClient(token[..^4] + "xxxx");

        using var res = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoints_WithoutToken_Return401()
    {
        await Fx.ResetAsync();
        using var client = AnonClient();
        foreach (var path in new[]
        {
            "/api/v1/bookings", "/api/v1/bookings/my-bookings",
            "/api/v1/services", "/api/v1/staffs",
        })
        {
            using var res = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }
    }

    [Fact]
    public async Task InactiveUser_Login_IsRejectedWith401()
    {
        await Fx.ResetAsync();
        await AddUserAsync("locked_out", API.Constraints.UserRole.Customer, isActive: false);
        using var client = AnonClient();

        using var res = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { userName = "locked_out", password = "pw123" });

        // A deactivated account must not receive a usable token.
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
