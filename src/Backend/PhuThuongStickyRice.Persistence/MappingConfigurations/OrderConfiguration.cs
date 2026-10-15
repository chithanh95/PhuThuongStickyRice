using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.OrderNumber)
                .HasMaxLength(Order.OrderNumberMaxLength)
                .IsRequired();
            builder.HasIndex(x => x.OrderNumber).IsUnique();

            builder.Property(x => x.CustomerName).HasMaxLength(Order.CustomerNameMaxLength).IsRequired();
            builder.Property(x => x.CustomerPhone).HasMaxLength(Order.CustomerPhoneMaxLength).IsRequired();
            builder.Property(x => x.CustomerEmail).HasMaxLength(Order.CustomerEmailMaxLength);
            builder.Property(x => x.CustomerAddress).HasMaxLength(Order.CustomerAddressMaxLength);
            builder.Property(x => x.Notes).HasMaxLength(Order.NotesMaxLength);

            // Wave 6c (item 33): internal staff note. Nullable, never public.
            builder.Property(x => x.AdminNote).HasMaxLength(Order.AdminNoteMaxLength);

            // Nullable: self-pickup orders have no delivery address.
            builder.Property(x => x.FulfillmentMethod)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.PaymentMethod)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.PaymentStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAt).IsRequired();

            // Wave 6a (item 23): scheduled delivery slot.
            builder.Property(x => x.DeliverySlot).HasMaxLength(Order.DeliverySlotMaxLength);

            // Item 128/130/133.
            builder.Property(x => x.Source).HasMaxLength(Order.SourceMaxLength);
            builder.Property(x => x.MoMoMatched).IsRequired();
            builder.Property(x => x.RefundStatus)
                .HasConversion<int>()
                .IsRequired();
            builder.Property(x => x.RefundAmount).HasPrecision(18, 2).IsRequired();

            builder.ComplexProperty(x => x.TotalAmount, totalBuilder =>
            {
                totalBuilder.Property(x => x.Amount)
                    .HasColumnName("TotalAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();
                totalBuilder.Property(x => x.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.ComplexProperty(x => x.DeliveryFee, feeBuilder =>
            {
                feeBuilder.Property(x => x.Amount)
                    .HasColumnName("DeliveryFee")
                    .HasPrecision(18, 2)
                    .IsRequired();
                feeBuilder.Property(x => x.Currency)
                    .HasColumnName("DeliveryFeeCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.ComplexProperty(x => x.DiscountAmount, discountBuilder =>
            {
                discountBuilder.Property(x => x.Amount)
                    .HasColumnName("DiscountAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();
                discountBuilder.Property(x => x.Currency)
                    .HasColumnName("DiscountCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.ComplexProperty(x => x.LoyaltyDiscount, loyaltyBuilder =>
            {
                loyaltyBuilder.Property(x => x.Amount)
                    .HasColumnName("LoyaltyDiscount")
                    .HasPrecision(18, 2)
                    .IsRequired();
                loyaltyBuilder.Property(x => x.Currency)
                    .HasColumnName("LoyaltyDiscountCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.ComplexProperty(x => x.TierDiscount, tierBuilder =>
            {
                tierBuilder.Property(x => x.Amount)
                    .HasColumnName("TierDiscount")
                    .HasPrecision(18, 2)
                    .IsRequired();
                tierBuilder.Property(x => x.Currency)
                    .HasColumnName("TierDiscountCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.ComplexProperty(x => x.GiftFee, giftBuilder =>
            {
                giftBuilder.Property(x => x.Amount)
                    .HasColumnName("GiftFee")
                    .HasPrecision(18, 2)
                    .IsRequired();
                giftBuilder.Property(x => x.Currency)
                    .HasColumnName("GiftFeeCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.Property(x => x.LoyaltyPointsRedeemed).IsRequired();
            builder.Property(x => x.RequiresDeposit).IsRequired();
            builder.Property(x => x.IsGift).IsRequired();
            builder.Property(x => x.LuckySpinUsed).IsRequired();

            builder.Property(x => x.DiscountCode)
                .HasMaxLength(Order.DiscountCodeMaxLength);

            builder.Property(x => x.TaxCode).HasMaxLength(Order.TaxCodeMaxLength);
            builder.Property(x => x.CompanyName).HasMaxLength(Order.CompanyNameMaxLength);
            builder.Property(x => x.RecipientName).HasMaxLength(Order.RecipientNameMaxLength);
            builder.Property(x => x.RecipientPhone).HasMaxLength(Order.RecipientPhoneMaxLength);
            builder.Property(x => x.GiftMessage).HasMaxLength(Order.GiftMessageMaxLength);
            builder.Property(x => x.ReferralCodeUsed).HasMaxLength(Order.ReferralCodeMaxLength);

            // Customer-booked pickup shipper (ShipperPickup orders). Nullable;
            // distinct from the shop's assigned delivery shipper (ShipperId).
            builder.Property(x => x.PickupShipperName).HasMaxLength(Order.PickupShipperNameMaxLength);
            builder.Property(x => x.PickupShipperPhone).HasMaxLength(Order.PickupShipperPhoneMaxLength);
            builder.Property(x => x.PickedUpAt);

            // Pre-order scheduled pickup time (UTC). Null = ASAP.
            builder.Property(x => x.ScheduledFor);

            // Item 163: cash-collection flag. Defaults to false.
            builder.Property(x => x.CashCollected).IsRequired();
            builder.Property(x => x.CashCollectedAt);

            // Registered customer account that placed the order (nullable).
            builder.Property(x => x.CustomerUserId);
            builder.HasIndex(x => x.CustomerUserId);

            // Delivery shipper assignment (optional).
            builder.Property(x => x.ShipperId);
            builder.HasOne(x => x.Shipper)
                .WithMany()
                .HasForeignKey(x => x.ShipperId)
                .OnDelete(DeleteBehavior.SetNull);

            // Multi-branch foundation (item 109).
            builder.Property(x => x.BranchId);
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.Property(x => x.BranchName).HasMaxLength(Branch.NameMaxLength);

            // Checkout idempotency: same key retried within 24h returns the
            // existing order. NULLs stay distinct under a unique index.
            builder.Property(x => x.IdempotencyKey).HasMaxLength(Order.IdempotencyKeyMaxLength);
            builder.HasIndex(x => x.IdempotencyKey).IsUnique();

            // Auto-applied "giờ vàng" promotion.
            builder.Property(x => x.PromotionName).HasMaxLength(Order.PromotionNameMaxLength);
            builder.Property(x => x.PromotionDiscountAmount).HasPrecision(18, 2).IsRequired();

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Lines).AutoInclude();
        }
    }
}
