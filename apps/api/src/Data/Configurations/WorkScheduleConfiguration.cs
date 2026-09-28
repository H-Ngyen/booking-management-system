using API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data.Configurations;

public class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.WorkDate).IsRequired().HasColumnType("date");
        builder.Property(w => w.StartTime).IsRequired().HasColumnType("time");
        builder.Property(w => w.EndTime).IsRequired().HasColumnType("time");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_WorkSchedules_TimeRange", "\"StartTime\" < \"EndTime\""));

        builder.HasIndex(w => new { w.StaffId, w.WorkDate })
            .HasDatabaseName("IX_WorkSchedules_Staff_Date");

        builder.Property(w => w.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(w => w.UpdatedAt).HasDefaultValueSql("NOW()");
    }
}
