using API.Common;
using API.Constraints;
using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

/// <summary>
/// Development-only seed matching the requirements minimum (§9):
/// 1 admin, 2 customers, 2 staffs, 5 services, 7-day schedules, 10 bookings.
/// Dates are relative to today so the demo never goes stale. Idempotent.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(DataContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        var now = VnClock.Now;
        var users = new List<User>
        {
            new() { UserName = "admin", Email = "admin@booking.local", Role = UserRole.Admin, PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { UserName = "customer1", Email = "customer1@booking.local", Role = UserRole.Customer, PasswordHash = BCrypt.Net.BCrypt.HashPassword("customer123"), IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { UserName = "customer2", Email = "customer2@booking.local", Role = UserRole.Customer, PasswordHash = BCrypt.Net.BCrypt.HashPassword("customer123"), IsActive = true, CreatedAt = now, UpdatedAt = now },
        };
        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        var services = new List<Service>
        {
            new() { Name = "Cắt tóc nam", Description = "Tư vấn kiểu, cắt, gội và sấy tạo kiểu.", DurationMinutes = 45, Price = 120000, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Name = "Gội đầu dưỡng sinh", Description = "Gội, massage đầu vai gáy với thảo dược.", DurationMinutes = 30, Price = 80000, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Name = "Nhuộm tóc", Description = "Tư vấn màu, nhuộm và phục hồi sau nhuộm.", DurationMinutes = 120, Price = 450000, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Name = "Massage body", Description = "Massage toàn thân với tinh dầu, giảm căng cơ.", DurationMinutes = 60, Price = 250000, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Name = "Chăm sóc da mặt", Description = "Làm sạch sâu, đắp mặt nạ và dưỡng ẩm.", DurationMinutes = 90, Price = 300000, IsActive = false, CreatedAt = now, UpdatedAt = now },
        };
        db.Services.AddRange(services);

        var staffs = new List<Staff>
        {
            new() { FullName = "Nguyễn Văn An", Email = "an.nv@booking.local", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { FullName = "Trần Thị Bình", Email = "binh.tt@booking.local", IsActive = true, CreatedAt = now, UpdatedAt = now },
        };
        db.Staffs.AddRange(staffs);
        await db.SaveChangesAsync();

        var shifts = new[] { ("08:00", "12:00"), ("13:30", "17:30") };
        var schedules = new List<WorkSchedule>();
        foreach (var staff in staffs)
            for (var day = 0; day < 7; day++)
                foreach (var (start, end) in shifts)
                    schedules.Add(new WorkSchedule
                    {
                        StaffId = staff.Id,
                        WorkDate = DateOnly.FromDateTime(VnClock.Now.AddDays(day)),
                        StartTime = TimeOnly.Parse(start),
                        EndTime = TimeOnly.Parse(end),
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
        db.WorkSchedules.AddRange(schedules);
        await db.SaveChangesAsync();

        var durations = services.ToDictionary(s => s.Id, s => s.DurationMinutes);
        // Booking codes mirror GenerateBookingCodeAsync: BK-YYYYMMDD-XXXXXX.
        // Fixed Random seed keeps demo data identical on every fresh seed.
        var codeRandom = new Random(42);
        const string codeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var usedCodes = new HashSet<string>();
        string NewCode(DateTime start)
        {
            string code;
            do
            {
                var suffix = new string(Enumerable.Range(0, 6)
                    .Select(_ => codeChars[codeRandom.Next(codeChars.Length)]).ToArray());
                code = $"BK-{start:yyyyMMdd}-{suffix}";
            } while (!usedCodes.Add(code));
            return code;
        }
        var seeds = new (int Customer, int Service, int Staff, int DayOffset, string Start, BookingStatus Status, string? Note, string? Reason)[]
        {
            (users[1].Id, services[0].Id, staffs[0].Id, -3, "09:00", BookingStatus.Completed, "Cắt ngắn hai bên", null),
            (users[2].Id, services[3].Id, staffs[1].Id, -1, "14:00", BookingStatus.Completed, null, null),
            (users[1].Id, services[0].Id, staffs[0].Id, 0, "10:30", BookingStatus.Confirmed, null, null),
            (users[1].Id, services[1].Id, staffs[1].Id, 1, "09:00", BookingStatus.Pending, "Gội nhẹ nhàng", null),
            (users[2].Id, services[2].Id, staffs[0].Id, 1, "13:30", BookingStatus.Pending, null, null),
            (users[1].Id, services[3].Id, staffs[1].Id, 2, "15:00", BookingStatus.Confirmed, null, null),
            (users[2].Id, services[0].Id, staffs[1].Id, 3, "08:30", BookingStatus.Pending, null, null),
            (users[1].Id, services[1].Id, staffs[0].Id, 1, "15:30", BookingStatus.Cancelled, null, "Bận đột xuất"),
            (users[2].Id, services[3].Id, staffs[0].Id, -2, "10:00", BookingStatus.Cancelled, null, "Đổi sang hôm khác"),
            (users[1].Id, services[2].Id, staffs[1].Id, 4, "13:30", BookingStatus.Confirmed, "Màu nâu hạt dẻ", null),
        };
        foreach (var (customer, service, staff, offset, start, status, note, reason) in seeds)
        {
            var date = VnClock.Now.AddDays(offset).Date;
            var parts = start.Split(':');
            var startTime = new DateTime(date.Year, date.Month, date.Day,
                int.Parse(parts[0]), int.Parse(parts[1]), 0, DateTimeKind.Unspecified);
            db.Bookings.Add(new Booking
            {
                BookingCode = NewCode(startTime),
                CustomerId = customer,
                ServiceId = service,
                StaffId = staff,
                StartTime = startTime,
                EndTime = startTime.AddMinutes(durations[service]),
                Status = status,
                CustomerNote = note,
                CancellationReason = reason,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        await db.SaveChangesAsync();
    }
}
