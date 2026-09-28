using System.Net;
using System.Net.Http.Json;
using API.DTOs;
using Api.Tests.Support;

namespace Api.Tests.Services;

// §2 Services management.
public class ServicesTests(DbFixture fx) : ApiTestBase(fx)
{
    private async Task<(HttpClient Admin, HttpClient Customer)> ClientsAsync()
    {
        await Fx.ResetAsync();
        await SeedUsersAsync();
        using var anon = AnonClient();
        var admin = AuthedClient(await LoginAsync(anon, "admin", "pw123"));
        var customer = AuthedClient(await LoginAsync(anon, "customer1", "pw123"));
        return (admin, customer);
    }

    private static Task<ApiTestBase.TestPage<ServiceDto>> ReadPaged(HttpResponseMessage res) =>
        ApiTestBase.ReadPage<ServiceDto>(res);

    [Fact]
    public async Task Admin_CreateService_Returns201WithId()
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.PostAsJsonAsync("/api/v1/services",
            new { name = "New Service", description = "d", durationMinutes = 30, price = 50000 });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var id = await res.Content.ReadFromJsonAsync<int>(Json);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task Customer_CreateService_Returns403()
    {
        var (_, customer) = await ClientsAsync();
        using var res = await customer.PostAsJsonAsync("/api/v1/services",
            new { name = "X", durationMinutes = 30, price = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Theory]
    [InlineData("", 30, 100)]      // missing name
    [InlineData("Ok", 0, 100)]     // duration must be > 0
    [InlineData("Ok", 30, -1)]     // price must not be negative
    public async Task Create_InvalidInput_Returns400(string name, int duration, int price)
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.PostAsJsonAsync("/api/v1/services",
            new { name, durationMinutes = duration, price });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Admin_UpdateService_Returns204AndPersists()
    {
        var (admin, _) = await ClientsAsync();
        var id = await (await admin.PostAsJsonAsync("/api/v1/services",
            new { name = "Orig", durationMinutes = 30, price = 100 })).Content.ReadFromJsonAsync<int>(Json);

        using var upd = await admin.PutAsJsonAsync($"/api/v1/services/{id}",
            new { name = "Renamed", description = "nd", durationMinutes = 45, price = 200, isActive = true });
        Assert.Equal(HttpStatusCode.NoContent, upd.StatusCode);

        using var list = await admin.GetAsync("/api/v1/services?pageNumber=1&pageSize=50");
        var paged = await ReadPaged(list);
        Assert.Contains(paged.Items, s => s.Id == id && s.Name == "Renamed" && s.DurationMinutes == 45);
    }

    [Fact]
    public async Task Update_NonexistentService_Returns404()
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.PutAsJsonAsync("/api/v1/services/999999",
            new { name = "X", durationMinutes = 30, price = 1, isActive = true });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Customer_UpdateService_Returns403()
    {
        var (admin, customer) = await ClientsAsync();
        var id = await (await admin.PostAsJsonAsync("/api/v1/services",
            new { name = "S", durationMinutes = 30, price = 1 })).Content.ReadFromJsonAsync<int>(Json);
        using var res = await customer.PutAsJsonAsync($"/api/v1/services/{id}",
            new { name = "Hacked", durationMinutes = 30, price = 1, isActive = true });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task LockedService_HiddenFromCustomer_VisibleToAdmin()
    {
        var (admin, customer) = await ClientsAsync();
        var id = await (await admin.PostAsJsonAsync("/api/v1/services",
            new { name = "ToLock", durationMinutes = 30, price = 1 })).Content.ReadFromJsonAsync<int>(Json);
        using var lockRes = await admin.PutAsJsonAsync($"/api/v1/services/{id}",
            new { name = "ToLock", durationMinutes = 30, price = 1, isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, lockRes.StatusCode);

        var custPaged = await ReadPaged(await customer.GetAsync("/api/v1/services?pageNumber=1&pageSize=50"));
        var adminPaged = await ReadPaged(await admin.GetAsync("/api/v1/services?pageNumber=1&pageSize=50"));
        Assert.DoesNotContain(custPaged.Items, s => s.Id == id);
        Assert.Contains(adminPaged.Items, s => s.Id == id && !s.IsActive);
    }

    [Fact]
    public async Task List_SearchAndPagination_WorkAtDatabase()
    {
        var (admin, _) = await ClientsAsync();
        foreach (var n in new[] { "Alpha Cut", "Alpha Color", "Beta Wash" })
            await admin.PostAsJsonAsync("/api/v1/services",
                new { name = n, durationMinutes = 30, price = 1 });

        var page1 = await ReadPaged(await admin.GetAsync("/api/v1/services?searchPhrase=Alpha&pageNumber=1&pageSize=1"));
        Assert.Equal(2, page1.TotalItemsCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Single(page1.Items);

        var page2 = await ReadPaged(await admin.GetAsync("/api/v1/services?searchPhrase=Alpha&pageNumber=2&pageSize=1"));
        Assert.Single(page2.Items);
        Assert.NotEqual(page1.Items.First().Id, page2.Items.First().Id);
    }

    [Fact]
    public async Task List_ResponseShape_MatchesFrontendAdapterContract()
    {
        var (admin, _) = await ClientsAsync();
        using var res = await admin.GetAsync("/api/v1/services?pageNumber=1&pageSize=5");
        var body = await res.Content.ReadAsStringAsync();
        foreach (var prop in new[] { "items", "totalPages", "totalItemsCount", "itemsFrom", "itemsTo" })
            Assert.Contains($"\"{prop}\"", body);
    }
}
