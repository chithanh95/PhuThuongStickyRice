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
            builder.Property(x => x.BankTransferExpiryMinutes).IsRequired();
            builder.Property(x => x.PointsPerVnd).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.PointsRedeemUnit).IsRequired();
            builder.Property(x => x.PointsRedeemValue).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.ReferralBonusPoints).IsRequired();
            builder.Property(x => x.DepositThreshold).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.DepositPercent).HasPrecision(5, 2).IsRequired();
            builder.Property(x => x.GiftFee).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.TierSilverMin).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.TierSilverDiscount).HasPrecision(5, 2).IsRequired();
            builder.Property(x => x.TierGoldMin).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.TierGoldDiscount).HasPrecision(5, 2).IsRequired();
            builder.Property(x => x.OwnerReportEmail).HasMaxLength(254);
            builder.Property(x => x.UnconfirmedAlertMinutes).IsRequired();
            builder.Property(x => x.RecaptchaSiteKey).HasMaxLength(100);
            builder.Property(x => x.RecaptchaSecretKey).HasMaxLength(100);
            builder.Property(x => x.WebBaseUrl).HasMaxLength(500);
        }
    }
}
