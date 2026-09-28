using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Api.Tests.Support;

namespace Api.Tests.Realtime;

// Realtime hub: negotiate auth + true push flow over LongPolling transport
// (TestServer has no WebSocket support; the client library is identical).
public class RealtimeTests(DbFixture fx) : ApiTestBase(fx)
{
    [Fact]
    public async Task Negotiate_WithoutToken_Returns401()
    {
        await Fx.ResetAsync();
        using var client = AnonClient();
        using var res = await client.PostAsync("/hubs/bookings/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task BookingCreated_PushesEventToOwnerConnection()
    {
        await Fx.ResetAsync();
        await SeedUsersAsync();
        using var anon = AnonClient();
        var custToken = await LoginAsync(anon, "customer1", "pw123");
        var adminToken = await LoginAsync(anon, "admin", "pw123");
        using var admin = AuthedClient(adminToken);

        var svcId = await (await admin.PostAsJsonAsync("/api/v1/services",
            new { name = "Svc", durationMinutes = 30, price = 1 })).Content.ReadFromJsonAsync<int>(Json);
        var staffId = await (await admin.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "St", email = $"{Guid.NewGuid():N}@t.local" })).Content.ReadFromJsonAsync<int>(Json);
        var date = TomorrowPlus(4).ToString("yyyy-MM-dd");
        using var sch = await admin.PostAsJsonAsync($"/api/v1/staffs/{staffId}/schedules",
            new { workDate = date, startTime = "08:00", endTime = "18:00" });
        sch.EnsureSuccessStatusCode();

        var received = new List<JsonElement>();
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(Fx.Factory.Server.BaseAddress, "hubs/bookings"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => Fx.Factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(custToken);
            })
            .Build();
        connection.On<JsonElement>("BookingChanged", e =>
        {
            lock (received) received.Add(e);
            connected.TrySetResult();
        });
        await connection.StartAsync();
        Assert.Equal(Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected, connection.State);

        var slots = await (await AuthedClient(custToken).GetAsync(
            $"/api/v1/bookings/available-slots?serviceId={svcId}&staffId={staffId}&date={date}"))
            .Content.ReadFromJsonAsync<List<string>>(Json);
        using var created = await AuthedClient(custToken).PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = svcId, staffId, startTime = slots![0] });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        await Task.WhenAny(connected.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.True(connected.Task.IsCompleted, "Expected BookingChanged push within 15s");
        Assert.Equal("Created", received[0].GetProperty("changeType").GetString());
        Assert.True(received[0].GetProperty("bookingId").GetInt32() > 0);

        await connection.StopAsync();
    }
}
