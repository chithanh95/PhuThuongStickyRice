using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class CheckInStreakConfiguration : IEntityTypeConfiguration<CheckInStreak>
    {
        public void Configure(EntityTypeBuilder<CheckInStreak> builder)
        {
            builder.ToTable("CheckInStreaks");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");
            builder.Property(x => x.Phone).HasMaxLength(CheckInStreak.PhoneMaxLength).IsRequired();
            builder.Property(x => x.CheckInDate).HasColumnType("date").IsRequired();
            // One check-in per phone per day.
            builder.HasIndex(x => new { x.Phone, x.CheckInDate }).IsUnique();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
