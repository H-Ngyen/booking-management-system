using API.Common;
using API.Jobs;
using Hangfire;
using Hangfire.PostgreSql;

namespace API.Extensions;

public static class HangfireExtensions
{
    public const string DashboardPath = "/hangfire";
    public const string OverdueJobId = "cancel-overdue-bookings";

    public static void AddHangfireInfrastructure(this WebApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));
        builder.Services.AddHangfireServer(o => o.WorkerCount = 2);
    }

    public static void UseHangfireInfrastructure(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.UseHangfireDashboard(DashboardPath);

        using var scope = app.Services.CreateScope();
        var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurringJobs.AddOrUpdate<OverdueBookingJob>(
            OverdueJobId,
            job => job.ExecuteAsync(),
            "0 0 * * *",
            new RecurringJobOptions { TimeZone = VnClock.Zone });
    }
}
