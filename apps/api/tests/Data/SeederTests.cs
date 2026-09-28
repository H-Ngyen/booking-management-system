using API.Constraints;
using API.Data;
using API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Api.Tests.Support;

namespace Api.Tests.Data;

// §9 seed minimums, tested directly (seeder only runs in Development;
// asserting it here guards the §9 contract in CI).
public class SeederTests(DbFixture fx) : ApiTestBase(fx)
{
    [Fact]
    public async Task Seed_ProducesRequirementMinimums()
    {
        await Fx.ResetAsync();
        using (var seedScope = Fx.CreateScope())
            await DbSeeder.SeedAsync(seedScope.ServiceProvider.GetRequiredService<DataContext>());

        using var scope = Fx.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        var users = await db.Users.ToListAsync();
        Assert.Single(users, u => u.Role == UserRole.Admin);
        Assert.Equal(2, users.Count(u => u.Role == UserRole.Customer));
        Assert.Equal(3, users.Count);

        Assert.Equal(5, await db.Services.CountAsync());
        Assert.Equal(2, await db.Staffs.CountAsync());

        var schedules = await db.WorkSchedules.ToListAsync();
        Assert.True(schedules.Count >= 14, $"Expected >= 14 shifts (7 days x 2 shifts x staffs), got {schedules.Count}");
        Assert.True(schedules.Select(s => s.WorkDate).Distinct().Count() >= 7, "Schedules must span 7 days");

        var bookings = await db.Bookings.ToListAsync();
        Assert.True(bookings.Count >= 10, $"Expected >= 10 bookings, got {bookings.Count}");
        var statuses = bookings.Select(b => b.Status).Distinct().ToList();
        foreach (var s in new[] { BookingStatus.Pending, BookingStatus.Confirmed, BookingStatus.Completed, BookingStatus.Cancelled })
            Assert.Contains(s, statuses);

        // Every booking references a real customer (FK integrity of the seed).
        Assert.All(bookings, b => Assert.Contains(users, u => u.Id == b.CustomerId));
    }

    [Fact]
    public async Task Seed_IsIdempotent_RerunChangesNothing()
    {
        await Fx.ResetAsync();
        using (var scope = Fx.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            await DbSeeder.SeedAsync(db);
            var before = await db.Bookings.CountAsync();
            await DbSeeder.SeedAsync(db);
            Assert.Equal(before, await db.Bookings.CountAsync());
        }
    }

    [Fact]
    public async Task SeededCredentials_CanLogin()
    {
        await Fx.ResetAsync();
        using (var scope = Fx.CreateScope())
            await DbSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<DataContext>());

        using var client = AnonClient(); // §9: credentials must work as documented
        Assert.False(string.IsNullOrWhiteSpace(await LoginAsync(client, "admin", "admin123")));
        Assert.False(string.IsNullOrWhiteSpace(await LoginAsync(client, "customer1", "customer123")));
    }
}
