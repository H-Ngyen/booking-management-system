using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.Common;
using API.Constraints;
using API.Data;
using API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Support;

// Shared Arrange helpers. Every test starts with ResetAsync (clean tables) and
// seeds only what it needs: Arrange is explicit per test (principle: isolated,
// deterministic, no hidden shared state).
[Collection("Db")]
public abstract class ApiTestBase
{
    protected readonly DbFixture Fx;
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    protected ApiTestBase(DbFixture fx) => Fx = fx;

    protected HttpClient AnonClient() => Fx.Factory.CreateClient();

    protected async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        using var res = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { userName, password });
        res.EnsureSuccessStatusCode();
        // Login returns ActionResult<string>: ASP.NET serves it as text/plain
        // (StringOutputFormatter), NOT a JSON string. Same contract the web
        // frontend relies on (axios reads raw text).
        return (await res.Content.ReadAsStringAsync()).Trim().Trim('"');
    }

    protected HttpClient AuthedClient(string token)
    {
        var client = Fx.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ---- direct seeding (bypasses API validation where the test needs to) ----

    protected async Task<User> AddUserAsync(string userName, UserRole role, bool isActive = true)
    {
        using var scope = Fx.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var now = VnClock.Now;
        var user = new User
        {
            UserName = userName,
            Email = $"{userName}@test.local",
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pw123", workFactor: 4),
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    protected async Task<(User Admin, User Customer1, User Customer2)> SeedUsersAsync()
    {
        var admin = await AddUserAsync("admin", UserRole.Admin);
        var c1 = await AddUserAsync("customer1", UserRole.Customer);
        var c2 = await AddUserAsync("customer2", UserRole.Customer);
        return (admin, c1, c2);
    }

    protected async Task<Service> AddServiceAsync(string name = "Test Service",
        int durationMinutes = 60, bool isActive = true)
    {
        using var scope = Fx.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var now = VnClock.Now;
        var service = new Service
        {
            Name = name,
            Description = "seeded",
            DurationMinutes = durationMinutes,
            Price = 100000,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Services.Add(service);
        await db.SaveChangesAsync();
        return service;
    }

    protected async Task<Staff> AddStaffAsync(string fullName = "Test Staff", bool isActive = true)
    {
        using var scope = Fx.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var now = VnClock.Now;
        var staff = new Staff
        {
            FullName = fullName,
            Email = $"{Guid.NewGuid():N}@test.local",
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Staffs.Add(staff);
        await db.SaveChangesAsync();
        return staff;
    }

    protected async Task<WorkSchedule> AddScheduleAsync(int staffId, DateOnly date,
        string start = "08:00", string end = "12:00")
    {
        using var scope = Fx.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var now = VnClock.Now;
        var schedule = new WorkSchedule
        {
            StaffId = staffId,
            WorkDate = date,
            StartTime = TimeOnly.Parse(start),
            EndTime = TimeOnly.Parse(end),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.WorkSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return schedule;
    }

    protected async Task<Booking> AddBookingAsync(int customerId, int serviceId, int staffId,
        DateTime start, int durationMinutes, BookingStatus status = BookingStatus.Pending)
    {
        using var scope = Fx.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var now = VnClock.Now;
        var booking = new Booking
        {
            BookingCode = $"BK-TST-{Guid.NewGuid():N}"[..20],
            CustomerId = customerId,
            ServiceId = serviceId,
            StaffId = staffId,
            StartTime = start,
            EndTime = start.AddMinutes(durationMinutes),
            Status = status,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    protected async Task<int> CountAsync<T>() where T : class
    {
        using var scope = Fx.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DataContext>().Set<T>().CountAsync();
    }

    protected static DateTime At(DateOnly date, string hhmm) =>
        date.ToDateTime(TimeOnly.Parse(hhmm));

    protected static DateOnly TomorrowPlus(int days) =>
        DateOnly.FromDateTime(VnClock.Now.AddDays(days));

    // API's PagedResult<T> has a parameterized ctor whose parameter names do
    // not match the JSON contract, so it cannot be deserialized back directly.
    // Tests read the wire shape (the same contract the frontend adapter uses).
    protected record TestPage<T>(List<T> Items, int TotalItemsCount, int TotalPages);

    protected static async Task<TestPage<T>> ReadPage<T>(HttpResponseMessage res)
    {
        using var doc = await res.Content.ReadFromJsonAsync<JsonDocument>(Json);
        var root = doc!.RootElement;
        return new TestPage<T>(
            root.GetProperty("items").Deserialize<List<T>>(Json)!,
            root.GetProperty("totalItemsCount").GetInt32(),
            root.GetProperty("totalPages").GetInt32());
    }
}

file static class HttpAssertions
{
    public static void Status(HttpResponseMessage res, System.Net.HttpStatusCode expected) =>
        Assert.Equal(expected, res.StatusCode);
}
