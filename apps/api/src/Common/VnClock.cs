namespace API.Common;

/// <summary>
/// Single source of "now" for the whole domain: Vietnam wall time (UTC+7,
/// no DST — the fixed offset is safe forever). All domain DateTimes are
/// VN wall time with Kind=Unspecified, stored in `timestamp without time
/// zone` columns. No timezone conversions anywhere in business logic.
/// Infrastructure concerns (JWT expiry) intentionally stay on UTC.
/// </summary>
public static class VnClock
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public static DateTime Now =>
        DateTime.SpecifyKind(DateTime.UtcNow.Add(Offset), DateTimeKind.Unspecified);

    public static DateOnly Today => DateOnly.FromDateTime(Now);

    // Hangfire resolves timezone IDs against the OS zoneinfo database, so a
    // custom zone ("VN") crashes startup. Use the real system zone instead:
    // Linux containers/dev machines carry "Asia/Ho_Chi_Minh", Windows carries
    // "SE Asia Standard Time". Both are fixed +07:00 (Vietnam has no DST).
    public static TimeZoneInfo Zone { get; } = FindVnZone();

    private static TimeZoneInfo FindVnZone()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        throw new InvalidOperationException(
            "No Vietnam timezone found on this machine (tried 'Asia/Ho_Chi_Minh', " +
            "'SE Asia Standard Time'). Install tzdata.");
    }
}
