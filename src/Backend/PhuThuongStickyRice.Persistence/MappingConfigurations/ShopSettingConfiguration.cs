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
            // Item 136: encrypted at rest ("DPv1:" + DataProtection payload) — needs headroom.
            builder.Property(x => x.RecaptchaSecretKey).HasMaxLength(1000);
            builder.Property(x => x.WebBaseUrl).HasMaxLength(500);

            // Wave 4: maintenance mode, health alerts, disk warning, payment
            // display config (non-secret), image backup.
            builder.Property(x => x.MaintenanceMode).IsRequired();
            builder.Property(x => x.MaintenanceMessage).HasMaxLength(500);
            // Item 136: encrypted at rest ("DPv1:" + DataProtection payload) — needs headroom.
            builder.Property(x => x.TelegramBotToken).HasMaxLength(1000);
            builder.Property(x => x.TelegramChatId).HasMaxLength(50);
            builder.Property(x => x.HealthAlertsEnabled).IsRequired();
            builder.Property(x => x.ErrorRateThreshold).IsRequired();
            builder.Property(x => x.UploadsSizeWarningMb).IsRequired();
            builder.Property(x => x.BankName).HasMaxLength(200);
            builder.Property(x => x.BankId).HasMaxLength(20);
            builder.Property(x => x.BankAccountNumber).HasMaxLength(50);
            builder.Property(x => x.BankAccountName).HasMaxLength(200);
            builder.Property(x => x.BankQrTemplate).HasMaxLength(20);
            builder.Property(x => x.MoMoPartnerCode).HasMaxLength(50);
            builder.Property(x => x.MoMoEndpoint).HasMaxLength(500);
            builder.Property(x => x.ImageBackupProvider).HasMaxLength(50);
            builder.Property(x => x.ImageBackupSettingsJson).HasMaxLength(4000);
            builder.Property(x => x.CheckInPoints).IsRequired();
            builder.Property(x => x.NepPerPortionGrams).IsRequired();
            builder.Property(x => x.DailyTargetAmount).HasPrecision(18, 2).IsRequired();

            // Wave 6a (item 22): open-hours display ("HH:mm").
            builder.Property(x => x.OpenTime).HasMaxLength(5);
            builder.Property(x => x.CloseTime).HasMaxLength(5);

            // Wave 6b (items 27, 30): floating contact numbers + pause-orders toggle.
            builder.Property(x => x.Hotline).HasMaxLength(30);
            builder.Property(x => x.ZaloPhone).HasMaxLength(30);
            builder.Property(x => x.AcceptingOrders).IsRequired();
            builder.Property(x => x.PausedReason).HasMaxLength(500);

            // Wave 6d (item 39): anti-spam per-phone daily cap.
            builder.Property(x => x.MaxOrdersPerPhonePerDay).IsRequired();
        }
    }
}
