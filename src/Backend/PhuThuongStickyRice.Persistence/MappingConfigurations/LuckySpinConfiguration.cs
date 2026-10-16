using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class LuckySpinConfiguration : IEntityTypeConfiguration<LuckySpin>
    {
        public void Configure(EntityTypeBuilder<LuckySpin> builder)
        {
            builder.ToTable("LuckySpins");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Phone).HasMaxLength(LuckySpin.PhoneMaxLength).IsRequired();
            // One spin per phone per day.
            builder.HasIndex(x => new { x.Phone, x.SpinDate }).IsUnique();

            builder.Property(x => x.SpinDate).HasColumnType("date").IsRequired();
            builder.Property(x => x.PrizeLabel).HasMaxLength(LuckySpin.PrizeLabelMaxLength).IsRequired();
            builder.Property(x => x.PrizeValue).IsRequired();
            builder.Property(x => x.VoucherCode).HasMaxLength(LuckySpin.VoucherCodeMaxLength).IsRequired();
            builder.HasIndex(x => x.VoucherCode).IsUnique();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
