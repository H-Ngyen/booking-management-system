using API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingCode)
            .IsRequired()
            .HasMaxLength(32);
        builder.HasIndex(b => b.BookingCode).IsUnique();

        builder.Property(b => b.StartTime).IsRequired();
        builder.Property(b => b.EndTime).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Bookings_TimeRange", "\"StartTime\" < \"EndTime\""));

        builder.Property(b => b.Status).IsRequired();

        builder.Property(b => b.CustomerNote).HasMaxLength(1000);
        builder.Property(b => b.CancellationReason).HasMaxLength(1000);

        builder.Property(b => b.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(b => b.UpdatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(b => new { b.StaffId, b.StartTime, b.EndTime, b.Status })
            .HasDatabaseName("IX_Bookings_Staff_Time");

        // Only non-cancelled bookings occupy a slot (Status <> 3 = Cancelled).
        builder.HasIndex(b => new { b.StaffId, b.StartTime, b.EndTime })
            .HasDatabaseName("IX_Bookings_Active_Overlap")
            .HasFilter("\"Status\" <> 3");

        builder.HasIndex(b => new { b.CustomerId, b.StartTime })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Bookings_Customer_Time");

        builder.HasIndex(b => new { b.Status, b.StartTime })
            .HasDatabaseName("IX_Bookings_Status_Date");
    }
}
