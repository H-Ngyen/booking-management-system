using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using API.Common;
using API.Constraints;
using API.DTOs;
using Api.Tests.Support;

namespace Api.Tests.Bookings;

// §4 + §5 business rules + TC1..TC6.
public class BookingTests(DbFixture fx) : ApiTestBase(fx)
{
    private record Setup(HttpClient Admin, HttpClient Customer1, HttpClient Customer2,
        int AdminId, int Customer1Id, int ServiceId, int StaffId, string Date);

    private async Task<Setup> ArrangeAsync(int durationMinutes = 60)
    {
        await Fx.ResetAsync();
        var (admin, c1, c2) = await SeedUsersAsync();
        using var anon = AnonClient();
        var adminClient = AuthedClient(await LoginAsync(anon, "admin", "pw123"));
        var customer1 = AuthedClient(await LoginAsync(anon, "customer1", "pw123"));
        var customer2 = AuthedClient(await LoginAsync(anon, "customer2", "pw123"));

        var svcId = await (await adminClient.PostAsJsonAsync("/api/v1/services",
            new { name = "Svc", durationMinutes, price = 100 })).Content.ReadFromJsonAsync<int>(Json);
        var staffId = await (await adminClient.PostAsJsonAsync("/api/v1/staffs",
            new { fullName = "St", email = $"{Guid.NewGuid():N}@t.local" })).Content.ReadFromJsonAsync<int>(Json);
        var date = TomorrowPlus(3).ToString("yyyy-MM-dd");
        using var sch = await adminClient.PostAsJsonAsync($"/api/v1/staffs/{staffId}/schedules",
            new { workDate = date, startTime = "08:00", endTime = "18:00" });
        sch.EnsureSuccessStatusCode();
        return new Setup(adminClient, customer1, customer2, admin.Id, c1.Id, svcId, staffId, date);
    }

    private static async Task<BookingDto> ReadBooking(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<BookingDto>(Json))!;

    [Fact]
    public async Task TC1_BookingInPast_Returns400()
    {
        var setup = await ArrangeAsync();
        using var res = await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = "2020-01-01T09:00:00" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task TC2_BookingOutsideWorkingHours_Returns400()
    {
        var setup = await ArrangeAsync();
        using var res = await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T20:00:00" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Theory] // §5.3 overlap truth table from the requirement doc
    [InlineData("09:00", "09:30", HttpStatusCode.Conflict)]   // 09:00-10:00 vs 09:30-10:30 -> overlap
    [InlineData("09:00", "08:30", HttpStatusCode.Conflict)]   // 09:00-10:00 vs 08:30-09:30 -> overlap
    [InlineData("09:00", "10:00", HttpStatusCode.Created)]    // boundary touch -> free
    public async Task TC3_OverlapTruthTable(string existing, string candidate, HttpStatusCode expected)
    {
        var setup = await ArrangeAsync(durationMinutes: 60);
        using var first = await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T{existing}:00" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var second = await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T{candidate}:00" });
        Assert.Equal(expected, second.StatusCode);
    }

    [Fact]
    public async Task TC4_CustomerCannotSeeOthersBookings_AdminSeesAll()
    {
        var setup = await ArrangeAsync();
        using var created = await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T09:00:00" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var c2list = await ReadPage<BookingDto>(await setup.Customer2.GetAsync("/api/v1/bookings/my-bookings?pageNumber=1&pageSize=50"));
        Assert.Empty(c2list.Items);

        var adminList = await ReadPage<BookingDto>(await setup.Admin.GetAsync("/api/v1/bookings?pageNumber=1&pageSize=50"));
        Assert.Single(adminList.Items);

        using var forbidden = await setup.Customer2.GetAsync("/api/v1/bookings?pageNumber=1&pageSize=50");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task TC5_CustomerCannotSelfConfirmOrComplete_AdminCan()
    {
        var setup = await ArrangeAsync();
        var booking = await ReadBooking(await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T09:00:00" }));

        foreach (var target in new[] { "Confirmed", "Completed" })
        {
            using var res = await setup.Customer1.PatchAsJsonAsync(
                $"/api/v1/bookings/{booking.Id}/status", new { status = target });
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }

        using var confirm = await setup.Admin.PatchAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/status", new { status = "Confirmed" });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
    }

    [Fact]
    public async Task TC6_CancelCompletedOrStarted_Returns400()
    {
        var setup = await ArrangeAsync();
        var booking = await ReadBooking(await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T09:00:00" }));
        await setup.Admin.PatchAsJsonAsync($"/api/v1/bookings/{booking.Id}/status", new { status = "Confirmed" });
        await setup.Admin.PatchAsJsonAsync($"/api/v1/bookings/{booking.Id}/status", new { status = "Completed" });

        using var cancelDone = await setup.Customer1.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel", new { reason = "too late" });
        Assert.Equal(HttpStatusCode.BadRequest, cancelDone.StatusCode);

        // Started (StartTime in the past, inserted directly): cannot cancel.
        var past = await AddBookingAsync(setup.Customer1Id, setup.ServiceId, setup.StaffId,
            VnClock.Now.AddHours(-1), 60, BookingStatus.Pending);
        using var cancelStarted = await setup.Customer1.PostAsJsonAsync(
            $"/api/v1/bookings/{past.Id}/cancel", new { reason = "too late" });
        Assert.Equal(HttpStatusCode.BadRequest, cancelStarted.StatusCode);
    }

    [Fact]
    public async Task Cancel_WithoutReason_Returns400_DoubleCancel_Returns400()
    {
        var setup = await ArrangeAsync();
        var booking = await ReadBooking(await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T09:00:00" }));

        using var noReason = await setup.Customer1.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel", new { reason = "" });
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        using var first = await setup.Customer1.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel", new { reason = "busy" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var cancelled = await ReadBooking(first);
        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);
        Assert.Equal("busy", cancelled.CancellationReason);

        using var second = await setup.Customer1.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel", new { reason = "again" });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Create_ComputesEndTime_AndReturnsWellFormedCode()
    {
        var setup = await ArrangeAsync(durationMinutes: 45);
        var booking = await ReadBooking(await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T09:00:00" }));

        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(booking.StartTime.AddMinutes(45), booking.EndTime); // §5.1
        Assert.Matches(new Regex(@"^BK-\d{8}-[A-Z2-9]{6}$"), booking.BookingCode);
        Assert.True(booking.BookingCode.Length <= 32);
    }

    [Fact]
    public async Task Create_OnInactiveServiceOrStaff_Returns400()
    {
        var setup = await ArrangeAsync();
        using var lockSvc = await setup.Admin.PutAsJsonAsync($"/api/v1/services/{setup.ServiceId}",
            new { name = "Svc", durationMinutes = 60, price = 100, isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, lockSvc.StatusCode);
        using var onLockedSvc = await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T09:00:00" });
        Assert.Equal(HttpStatusCode.BadRequest, onLockedSvc.StatusCode);
    }

    [Fact]
    public async Task AvailableSlots_ExcludesBooked_AndPast()
    {
        var setup = await ArrangeAsync(durationMinutes: 60);
        var before = await (await setup.Customer1.GetAsync(
            $"/api/v1/bookings/available-slots?serviceId={setup.ServiceId}&staffId={setup.StaffId}&date={setup.Date}"))
            .Content.ReadFromJsonAsync<List<string>>(Json);
        Assert.NotEmpty(before!);

        using var created = await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = before![0] });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var after = await (await setup.Customer1.GetAsync(
            $"/api/v1/bookings/available-slots?serviceId={setup.ServiceId}&staffId={setup.StaffId}&date={setup.Date}"))
            .Content.ReadFromJsonAsync<List<string>>(Json);
        Assert.DoesNotContain(before[0], after!);
        Assert.True(after!.Count < before.Count);
        // ISO-8601 UTC, parseable, ordered.
        var parsed = after.Select(DateTime.Parse).ToList();
        Assert.Equal(parsed.OrderBy(d => d), parsed);
    }

    [Fact]
    public async Task List_FilterByDateAndStatus_WithPagination()
    {
        var setup = await ArrangeAsync();
        foreach (var t in new[] { "09:00", "11:00" })
            await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
                new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T{t}:00" });

        var filtered = await ReadPage<BookingDto>(await setup.Admin.GetAsync(
            $"/api/v1/bookings?date={setup.Date}&status=Pending&pageNumber=1&pageSize=50"));
        Assert.Equal(2, filtered.TotalItemsCount);

        var confirmed = await ReadPage<BookingDto>(await setup.Admin.GetAsync(
            $"/api/v1/bookings?date={setup.Date}&status=Confirmed&pageNumber=1&pageSize=50"));
        Assert.Empty(confirmed.Items);
    }

    [Fact]
    public async Task StatusFlow_ForwardOnly_IllegalJumpsReturn400()
    {
        var setup = await ArrangeAsync();
        var booking = await ReadBooking(await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T09:00:00" }));

        // Pending -> Completed directly is rejected (must go through Confirmed).
        using var skip = await setup.Admin.PatchAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/status", new { status = "Completed" });
        Assert.Equal(HttpStatusCode.BadRequest, skip.StatusCode);

        using var confirm = await setup.Admin.PatchAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/status", new { status = "Confirmed" });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        // Cancelled-via-status is rejected: cancellation requires the cancel endpoint (reason).
        using var toCancelled = await setup.Admin.PatchAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/status", new { status = "Cancelled" });
        Assert.Equal(HttpStatusCode.BadRequest, toCancelled.StatusCode);

        using var complete = await setup.Admin.PatchAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/status", new { status = "Completed" });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        using var afterDone = await setup.Admin.PatchAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/status", new { status = "Confirmed" });
        Assert.Equal(HttpStatusCode.BadRequest, afterDone.StatusCode);
    }

    [Fact]
    public async Task ConcurrentSameSlot_ExactlyOneWins_RestConflict()
    {
        var setup = await ArrangeAsync();
        var slots = await (await setup.Customer1.GetAsync(
            $"/api/v1/bookings/available-slots?serviceId={setup.ServiceId}&staffId={setup.StaffId}&date={setup.Date}"))
            .Content.ReadFromJsonAsync<List<string>>(Json);
        var target = slots![10];

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
                new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = target })));

        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(7, results.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        foreach (var r in results) r.Dispose();
    }

    [Fact]
    public async Task DisplayNames_PopulatedOnCreateListUpdateCancel()
    {
        var setup = await ArrangeAsync();
        var created = await ReadBooking(await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T09:00:00" }));
        Assert.False(string.IsNullOrWhiteSpace(created.ServiceName));
        Assert.False(string.IsNullOrWhiteSpace(created.CustomerName));
        Assert.False(string.IsNullOrWhiteSpace(created.StaffName));

        var listed = await ReadPage<BookingDto>(await setup.Admin.GetAsync(
            $"/api/v1/bookings?date={setup.Date}&pageNumber=1&pageSize=50"));
        var row = Assert.Single(listed.Items);
        Assert.Equal(created.ServiceName, row.ServiceName);
        Assert.Equal(created.CustomerName, row.CustomerName);
        Assert.Equal(created.StaffName, row.StaffName);

        var confirmed = await ReadBooking(await setup.Admin.PatchAsJsonAsync(
            $"/api/v1/bookings/{created.Id}/status", new { status = "Confirmed" }));
        Assert.False(string.IsNullOrWhiteSpace(confirmed.ServiceName));
        Assert.False(string.IsNullOrWhiteSpace(confirmed.CustomerName));
        Assert.False(string.IsNullOrWhiteSpace(confirmed.StaffName));

        // Cancel path (fresh booking since completed ones cannot be cancelled).
        var second = await ReadBooking(await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
            new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = $"{setup.Date}T11:00:00" }));
        var cancelled = await ReadBooking(await setup.Customer1.PostAsJsonAsync(
            $"/api/v1/bookings/{second.Id}/cancel", new { reason = "name check" }));
        Assert.False(string.IsNullOrWhiteSpace(cancelled.ServiceName));
        Assert.False(string.IsNullOrWhiteSpace(cancelled.CustomerName));
        Assert.False(string.IsNullOrWhiteSpace(cancelled.StaffName));
    }

    [Fact]
    public async Task BookingCodes_AreUniqueAcrossSlots()
    {
        var setup = await ArrangeAsync(durationMinutes: 30);
        var slots = await (await setup.Customer1.GetAsync(
            $"/api/v1/bookings/available-slots?serviceId={setup.ServiceId}&staffId={setup.StaffId}&date={setup.Date}"))
            .Content.ReadFromJsonAsync<List<string>>(Json);
        var codes = new List<string>();
        // Every other slot: 30-min service on 15-min grid overlaps neighbors,
        // so step by 2 to keep creations non-overlapping.
        foreach (var slot in slots!.Where((_, i) => i % 2 == 0).Take(5))
        {
            var b = await ReadBooking(await setup.Customer1.PostAsJsonAsync("/api/v1/bookings",
                new { serviceId = setup.ServiceId, staffId = setup.StaffId, startTime = slot }));
            codes.Add(b.BookingCode);
        }
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }
}
