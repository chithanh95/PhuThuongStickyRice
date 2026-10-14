using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class ShopSettingConfiguration : IEntityTypeConfiguration<ShopSetting>
    {
        public void Configure(EntityTypeBuilder<ShopSetting> builder)
        {
            builder.ToTable("ShopSettings");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.DeliveryFee).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.FreeShipThreshold).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.MinOrderAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.EstimatedDeliveryMinutes).IsRequired();
        }
    }
}
