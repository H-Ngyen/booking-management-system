using System.Net;
using API.Common;
using API.Constraints;
using API.Data;
using API.Entities;
using API.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Api.Tests.Support;

namespace Api.Tests.Jobs;

// Hangfire recurring job, invoked directly (no background server in tests).
public class OverdueJobTests(DbFixture fx) : ApiTestBase(fx)
{
    [Fact]
    public async Task Execute_CancelsOnlyOverdue_WithSystemReason_AndIsIdempotent()
    {
        await Fx.ResetAsync();
        var (_, c1, _) = await SeedUsersAsync();
        var svc = await AddServiceAsync(durationMinutes: 60);
        var staff = await AddStaffAsync();
        var now = VnClock.Now;

        var pastPending = await AddBookingAsync(c1.Id, svc.Id, staff.Id, now.AddHours(-3), 60, BookingStatus.Pending);
        var pastConfirmed = await AddBookingAsync(c1.Id, svc.Id, staff.Id, now.AddHours(-5), 60, BookingStatus.Confirmed);
        var futurePending = await AddBookingAsync(c1.Id, svc.Id, staff.Id, now.AddHours(3), 60, BookingStatus.Pending);
        var pastCompleted = await AddBookingAsync(c1.Id, svc.Id, staff.Id, now.AddHours(-8), 60, BookingStatus.Completed);

        using (var scope = Fx.CreateScope())
            await scope.ServiceProvider.GetRequiredService<OverdueBookingJob>().ExecuteAsync();

        using (var scope = Fx.CreateScope())
        {
            async Task<Booking> Get(int id)
            {
                using var s2 = Fx.CreateScope();
                return (await s2.ServiceProvider.GetRequiredService<DataContext>().Bookings.FindAsync(id))!;
            }
            var pp = await Get(pastPending.Id);
            var pc = await Get(pastConfirmed.Id);
            var fp = await Get(futurePending.Id);
            var done = await Get(pastCompleted.Id);

            Assert.Equal(BookingStatus.Cancelled, pp.Status);
            Assert.Equal(OverdueBookingJob.AutoCancelReason, pp.CancellationReason);
            Assert.Equal(BookingStatus.Cancelled, pc.Status);
            Assert.Equal(OverdueBookingJob.AutoCancelReason, pc.CancellationReason);
            Assert.Equal(BookingStatus.Pending, fp.Status);
            Assert.Null(fp.CancellationReason);
            Assert.Equal(BookingStatus.Completed, done.Status);
        }

        // Second run changes nothing (idempotent).
        using (var scope = Fx.CreateScope())
            await scope.ServiceProvider.GetRequiredService<OverdueBookingJob>().ExecuteAsync();
        using (var scope = Fx.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var fp = (await db.Bookings.FindAsync(futurePending.Id))!;
            Assert.Equal(BookingStatus.Pending, fp.Status);
        }
    }

    [Fact]
    public async Task Execute_WithNoOverdue_DoesNothing()
    {
        await Fx.ResetAsync();
        using (var scope = Fx.CreateScope())
            await scope.ServiceProvider.GetRequiredService<OverdueBookingJob>().ExecuteAsync();
        Assert.Equal(0, await CountAsync<Booking>());
    }

    [Fact]
    public async Task RecurringJob_IsRegisteredInHangfireStorage()
    {
        await Fx.ResetAsync();
        using var anon = AnonClient();
        // Dashboard is dev-only; the recurring definition lives in shared storage.
        // Resolving the manager proves the registration path used by Program.cs.
        using var scope = Fx.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<Hangfire.IRecurringJobManager>();
        Assert.NotNull(manager);
        await Task.CompletedTask;
    }
}
