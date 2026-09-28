using Api.Tests.Support;

namespace Api.Tests.Smoke;

public class SmokeTests(DbFixture fx) : ApiTestBase(fx)
{
    [Fact]
    public async Task Health_RespondsOk()
    {
        await Fx.ResetAsync();
        using var client = AnonClient();
        using var res = await client.GetAsync("/api/v1/Health");
        Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Login_SeededAdmin_ReturnsToken()
    {
        await Fx.ResetAsync();
        await SeedUsersAsync();
        using var client = AnonClient();
        var token = await LoginAsync(client, "admin", "pw123");
        Assert.False(string.IsNullOrWhiteSpace(token));
    }
}
