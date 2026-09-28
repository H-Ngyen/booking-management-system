using API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnType("citext");
        builder.HasIndex(s => s.Email).IsUnique();

        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("NOW()");

        builder.HasMany(s => s.Schedules)
            .WithOne(w => w.Staff)
            .HasForeignKey(w => w.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Bookings)
            .WithOne(b => b.Staff)
            .HasForeignKey(b => b.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
