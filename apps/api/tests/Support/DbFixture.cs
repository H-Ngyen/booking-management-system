using API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Api.Tests.Support;

// Single shared Postgres container + one in-process API host for the whole run.
// Per-test isolation is achieved by wiping domain tables (see DbFixture.ResetAsync).
// Hangfire's background server is removed for determinism; OverdueBookingJob is
// tested by direct invocation (OverdueJobTests).
public class DbFixture : IAsyncLifetime
{
    public const string JwtSecret =
        "test-secret-key-must-be-at-least-64-chars-long-for-hs512-0123456789abcdef";

    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("testdb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string ConnectionString => _pg.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        // Process env (double-underscore nesting): the most robust way to feed
        // config to WebApplicationFactory — honored regardless of how the app
        // builds its ConfigurationManager. LoadEnv() picks these up last, so
        // they win over appsettings files.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", JwtSecret);
        Environment.SetEnvironmentVariable("Frontend__Origin", "http://localhost:3000");

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            // No background workers in tests: recurring jobs are invoked directly.
            builder.ConfigureTestServices(services =>
                services.RemoveAll<IHostedService>());
        });

        // Apply EF migrations (Program only does this in Development).
        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _pg.DisposeAsync();
    }

    /// <summary>Wipes domain tables (child-first). Hangfire tables are kept.</summary>
    public async Task ResetAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE \"Bookings\", \"WorkSchedules\", \"Services\", \"Staffs\", \"Users\" RESTART IDENTITY;");
    }

    public IServiceScope CreateScope() => Factory.Services.CreateScope();
}

[CollectionDefinition("Db", DisableParallelization = true)]
public class DbCollection : ICollectionFixture<DbFixture>
{
}
