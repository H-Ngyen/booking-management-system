using API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description).HasMaxLength(1000);

        builder.Property(s => s.DurationMinutes).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Services_DurationMinutes", "\"DurationMinutes\" > 0"));

        builder.Property(s => s.Price)
            .IsRequired()
            .HasColumnType("numeric(18,2)");
        builder.ToTable(t => t.HasCheckConstraint("CK_Services_Price", "\"Price\" >= 0"));

        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("NOW()");

        builder.HasMany(s => s.Bookings)
            .WithOne(b => b.Service)
            .HasForeignKey(b => b.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
