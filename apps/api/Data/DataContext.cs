using API.Common;
using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<WorkSchedule> WorkSchedules { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Domain datetimes are VN wall time: store without timezone, no conversions.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            foreach (var prop in entity.GetProperties().Where(p => p.ClrType == typeof(DateTime)))
                prop.SetColumnType("timestamp without time zone");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = VnClock.Now;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not { } || entry.State is not (EntityState.Added or EntityState.Modified))
                continue;
            if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                entry.Property("UpdatedAt").CurrentValue = now;
            if (entry.State == EntityState.Added && entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
            {
                var current = (DateTime)entry.Property("CreatedAt").CurrentValue!;
                if (current == default)
                    entry.Property("CreatedAt").CurrentValue = now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
