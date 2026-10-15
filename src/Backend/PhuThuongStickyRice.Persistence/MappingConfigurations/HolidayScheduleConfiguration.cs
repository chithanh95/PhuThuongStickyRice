using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class HolidayScheduleConfiguration : IEntityTypeConfiguration<HolidaySchedule>
    {
        public void Configure(EntityTypeBuilder<HolidaySchedule> builder)
        {
            builder.ToTable("HolidaySchedules");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.HolidayDate)
                .HasColumnType("date")
                .IsRequired();
            builder.Property(x => x.Reason)
                .HasMaxLength(HolidaySchedule.ReasonMaxLength)
                .IsRequired();
            builder.Property(x => x.IsFullDay).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();

            builder.HasIndex(x => x.HolidayDate);
        }
    }
}
