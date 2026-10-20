// codequality-team 2026-10-20 run-0245 Agent2-CleanCode
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    /// <summary>
    /// Cấu hình EF Core cho bảng Orders (đơn hàng): cột, index, quan hệ và
    /// các cột tiền (Money complex type). Tách thành các method nhỏ theo nhóm
    /// cột để dễ đọc; thứ tự gọi không ảnh hưởng model cuối cùng.
    /// </summary>
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            ConfigureIdentity(builder);
            ConfigureCustomerFields(builder);
            ConfigureFulfillmentAndPayment(builder);
            ConfigureMoneyColumns(builder);
            ConfigureMarketingFields(builder);
            ConfigureInvoiceFields(builder);
            ConfigurePickupFields(builder);
            ConfigureCashCollection(builder);
            ConfigureLookupIndexes(builder);
            ConfigureRelationships(builder);
        }

        /// <summary>
        /// Bảng + khóa chính + mã đơn duy nhất (dùng tra cứu đơn public).
        /// </summary>
        private static void ConfigureIdentity(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.OrderNumber)
                .HasMaxLength(Order.OrderNumberMaxLength)
                .IsRequired();
            builder.HasIndex(x => x.OrderNumber).IsUnique();
        }

        /// <summary>
        /// Thông tin khách đặt: tên/SĐT bắt buộc, email/địa chỉ/ghi chú nullable,
        /// ghi chú nội bộ (AdminNote, không public), lý do hủy đơn (Item 233).
        /// SĐT chuẩn hóa 9 số cuối (CustomerPhoneNorm) có composite index để
        /// tra cứu khoan dung định dạng chạy seek trên index thay vì scan.
        /// </summary>
        private static void ConfigureCustomerFields(EntityTypeBuilder<Order> builder)
        {
            builder.Property(x => x.CustomerName).HasMaxLength(Order.CustomerNameMaxLength).IsRequired();
            builder.Property(x => x.CustomerPhone).HasMaxLength(Order.CustomerPhoneMaxLength).IsRequired();

            // perf-hunt 2026-10-19 (run 15:45): SĐT chuẩn hóa 9 số cuối để
            // các tra cứu khoan dung định dạng (HasOrderedBefore, giới hạn
            // đơn/ngày, voucher đơn đầu, bonus giới thiệu) chạy so sánh bằng
            // trên index thay vì LIKE '%...%' / EndsWith trên cột đã REPLACE
            // — quét toàn bảng Orders (bảng nóng nhất) mỗi lần checkout.
            builder.Property(x => x.CustomerPhoneNorm)
                .HasMaxLength(Order.CustomerPhoneNormMaxLength)
                .IsRequired();
            builder.HasIndex(x => new { x.CustomerPhoneNorm, x.Status });
            // perf-hunt 2026-10-19 (run 19:15): index đơn CustomerPhoneNorm bị
            // loại bỏ vì dư thừa — mọi truy vấn lọc bằng trên norm đều được
            // phục vụ qua prefix trái của 2 composite index dưới
            // (HasOrderedBefore / EnsurePhoneDailyLimit / CountCompletedOrders /
            // HasNoPriorCompletedOrder / tra cứu đơn theo SĐT đều lọc bằng
            // CustomerPhoneNorm trước khi so khoan dung trong memory).
            // (1) (CustomerPhoneNorm, Status): full seek 2 điều kiện bằng cho
            // LoyaltyAppService.HasNoPriorCompletedOrderAsync (mỗi khi đơn dùng
            // mã giới thiệu hoàn thành) và CountCompletedOrdersByPhoneAsync.
            // (2) (CustomerPhoneNorm, CreatedAt): seek theo SĐT + sort ngày tạo
            // ngay trên index cho 2 đường nóng public: tra cứu đơn theo SĐT
            // (LoadOrderCandidatesAsync) và prefill tên/địa chỉ checkout
            // (GetLastOrderInfoByPhoneAsync) — đều OrderByDescending(CreatedAt)
            // + Take(100). Thay index đơn dư thừa nên write cost không tăng.
            builder.HasIndex(x => new { x.CustomerPhoneNorm, x.CreatedAt });
            builder.Property(x => x.CustomerEmail).HasMaxLength(Order.CustomerEmailMaxLength);
            builder.Property(x => x.CustomerAddress).HasMaxLength(Order.CustomerAddressMaxLength);
            builder.Property(x => x.Notes).HasMaxLength(Order.NotesMaxLength);

            // Wave 6a (item 33): internal staff note. Nullable, never public.
            builder.Property(x => x.AdminNote).HasMaxLength(Order.AdminNoteMaxLength);

            // Item 233: lý do hủy đơn. Nullable — chỉ có giá trị với đơn đã hủy.
            builder.Property(x => x.CancelReason).HasMaxLength(Order.CancelReasonMaxLength);
            builder.HasIndex(x => x.CancelReason);
        }

        /// <summary>
        /// Hình thức nhận hàng, trạng thái đơn/thanh toán/hoàn tiền (lưu dạng
        /// int), khung giờ giao đã đặt (Wave 6a item 23), nguồn đơn và cờ đối
        /// soát MoMo (Item 128/130/133).
        /// </summary>
        private static void ConfigureFulfillmentAndPayment(EntityTypeBuilder<Order> builder)
        {
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
        }

        /// <summary>
        /// 6 cột tiền trên đơn: tổng tiền, phí ship, giảm giá mã, giảm giá
        /// loyalty, giảm giá hạng, phí gói quà. Tên cột giữ nguyên như cũ.
        /// Dùng <see cref="MoneyMappingHelper"/> chung với OrderLine/Product.
        /// </summary>
        private static void ConfigureMoneyColumns(EntityTypeBuilder<Order> builder)
        {
            MoneyMappingHelper.ConfigureMoneyColumn(builder, x => x.TotalAmount, "TotalAmount", "Currency");
            MoneyMappingHelper.ConfigureMoneyColumn(builder, x => x.DeliveryFee, "DeliveryFee", "DeliveryFeeCurrency");
            MoneyMappingHelper.ConfigureMoneyColumn(builder, x => x.DiscountAmount, "DiscountAmount", "DiscountCurrency");
            MoneyMappingHelper.ConfigureMoneyColumn(builder, x => x.LoyaltyDiscount, "LoyaltyDiscount", "LoyaltyDiscountCurrency");
            MoneyMappingHelper.ConfigureMoneyColumn(builder, x => x.TierDiscount, "TierDiscount", "TierDiscountCurrency");
            MoneyMappingHelper.ConfigureMoneyColumn(builder, x => x.GiftFee, "GiftFee", "GiftFeeCurrency");
        }

        /// <summary>
        /// Các trường marketing/khuyến mãi: điểm loyalty đã đổi, cọc, gói quà,
        /// vòng quay may mắn đã dùng, mã giảm giá + mã giới thiệu đã dùng,
        /// khuyến mãi "giờ vàng" tự áp.
        /// </summary>
        private static void ConfigureMarketingFields(EntityTypeBuilder<Order> builder)
        {
            builder.Property(x => x.LoyaltyPointsRedeemed).IsRequired();
            builder.Property(x => x.RequiresDeposit).IsRequired();
            builder.Property(x => x.IsGift).IsRequired();
            builder.Property(x => x.LuckySpinUsed).IsRequired();

            builder.Property(x => x.DiscountCode)
                .HasMaxLength(Order.DiscountCodeMaxLength);

            builder.Property(x => x.SpinPrizeCode)
                .HasMaxLength(Order.SpinPrizeCodeMaxLength);
            builder.Property(x => x.ReferralCodeUsed).HasMaxLength(Order.ReferralCodeMaxLength);

            // Auto-applied "giờ vàng" promotion.
            builder.Property(x => x.PromotionName).HasMaxLength(Order.PromotionNameMaxLength);
            builder.Property(x => x.PromotionDiscountAmount).HasPrecision(18, 2).IsRequired();
        }

        /// <summary>
        /// Thông tin xuất hóa đơn công ty + người nhận quà (tất cả nullable).
        /// </summary>
        private static void ConfigureInvoiceFields(EntityTypeBuilder<Order> builder)
        {
            builder.Property(x => x.TaxCode).HasMaxLength(Order.TaxCodeMaxLength);
            builder.Property(x => x.CompanyName).HasMaxLength(Order.CompanyNameMaxLength);
            builder.Property(x => x.RecipientName).HasMaxLength(Order.RecipientNameMaxLength);
            builder.Property(x => x.RecipientPhone).HasMaxLength(Order.RecipientPhoneMaxLength);
            builder.Property(x => x.GiftMessage).HasMaxLength(Order.GiftMessageMaxLength);
        }

        /// <summary>
        /// Shipper do khách tự đặt (ShipperPickup) — khác shipper quán phân
        /// (ShipperId); giờ hẹn lấy hàng đã đặt trước (ScheduledFor, UTC, null = nhận ngay).
        /// </summary>
        private static void ConfigurePickupFields(EntityTypeBuilder<Order> builder)
        {
            // Customer-booked pickup shipper (ShipperPickup orders). Nullable;
            // distinct from the shop's assigned delivery shipper (ShipperId).
            builder.Property(x => x.PickupShipperName).HasMaxLength(Order.PickupShipperNameMaxLength);
            builder.Property(x => x.PickupShipperPhone).HasMaxLength(Order.PickupShipperPhoneMaxLength);
            builder.Property(x => x.PickedUpAt);

            // Pre-order scheduled pickup time (UTC). Null = ASAP.
            builder.Property(x => x.ScheduledFor);
        }

        /// <summary>
        /// Thu hộ tiền mặt (Item 163): cờ + thời điểm thu, mặc định false.
        /// </summary>
        private static void ConfigureCashCollection(EntityTypeBuilder<Order> builder)
        {
            // Item 163: cash-collection flag. Defaults to false.
            builder.Property(x => x.CashCollected).IsRequired();
            builder.Property(x => x.CashCollectedAt);
        }

        /// <summary>
        /// Index tra cứu: tài khoản khách đăng ký đặt đơn, composite index
        /// (Status, CreatedAt) cho danh sách đơn admin, (CustomerPhone, Status),
        /// trạng thái hoàn tiền và (PaymentMethod, PaymentStatus) cho đối soát.
        /// </summary>
        private static void ConfigureLookupIndexes(EntityTypeBuilder<Order> builder)
        {
            // Registered customer account that placed the order (nullable).
            builder.Property(x => x.CustomerUserId);
            builder.HasIndex(x => x.CustomerUserId);
            builder.HasIndex(x => new { x.Status, x.CreatedAt });

            // perf-hunt 2026-10-19: phone-based lookups run on hot paths
            // (pay-later eligibility + referral checks at checkout, customer
            // order history, admin customer detail). Composite (CustomerPhone,
            // Status) serves both the phone+status and phone-only predicates.
            builder.HasIndex(x => new { x.CustomerPhone, x.Status });

            // perf-hunt 2026-10-19: báo cáo hoàn tiền (RefundAppService) lọc theo
            // RefundStatus; trang đối soát MoMo lọc (PaymentMethod, PaymentStatus).
            builder.HasIndex(x => x.RefundStatus);
            builder.HasIndex(x => new { x.PaymentMethod, x.PaymentStatus });

            // perf-hunt 2026-10-19: admin order list (OrderAppService.GetListAsync)
            // + order reports (OrderReportsAppService) filter Where(ShipperId == ...)
            // — equality on Guid without index = table scan on the hottest table.
            // Non-unique index: lookup only, no business change.
            builder.HasIndex(x => x.ShipperId);
        }

        /// <summary>
        /// Quan hệ: shipper quán phân (SetNull khi xóa), chi nhánh (item 109),
        /// idempotency key chống tạo trùng đơn khi checkout retry (unique, NULL
        /// vẫn phân biệt được), và các dòng đơn (cascade khi xóa đơn).
        /// </summary>
        private static void ConfigureRelationships(EntityTypeBuilder<Order> builder)
        {
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

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // P6: AutoInclude removed — Lines are loaded explicitly via
            // .Include() only where needed (FindOrThrowAsync for detail views).
            // List/dashboard queries use SelectMany (SQL JOIN) without loading
            // full entities.
        }
    }
}
