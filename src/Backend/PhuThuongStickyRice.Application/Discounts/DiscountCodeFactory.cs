using System;
using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Application.Discounts
{
    /// <summary>
    /// Dựng entity mã voucher cá nhân dùng chung. Gom bản copy-paste trước
    /// đây nằm ở <c>VoucherCampaignAppService.BuildVoucherCode</c> (mã chiến
    /// dịch) và <c>CustomerProfileAppService.InsertBirthdayDiscountCodeAsync</c>
    /// (quà sinh nhật) — cùng 6 field mặc định (dùng 1 lần, gắn SĐT nhận,
    /// MinOrderAmount 0, active...), chỉ khác cách tính số tiền/% giảm.
    /// Bên gọi tự resolve discountAmount/discountPercent rồi truyền vào.
    /// </summary>
    // [codequality-smell 2026-10-20] Gộp khối khởi tạo DiscountCode trùng nhau ở 2 service.
    internal static class DiscountCodeFactory
    {
        /// <summary>
        /// Dựng mã voucher cá nhân: dùng 1 lần, gắn đúng SĐT nhận để không
        /// chia sẻ/tái sử dụng; số tiền cố định và % giảm không tồn tại song
        /// song (checkout chỉ áp dụng 1 trong 2).
        /// </summary>
        /// <param name="code">Mã voucher.</param>
        /// <param name="phone">SĐT nhận voucher (OwnerPhone).</param>
        /// <param name="discountAmount">Số tiền giảm cố định, null nếu dùng %.</param>
        /// <param name="discountPercent">% giảm, null nếu dùng số tiền cố định.</param>
        /// <param name="expiresAt">Hạn dùng.</param>
        internal static DiscountCode CreatePersonalVoucher(
            string code,
            string phone,
            decimal? discountAmount,
            decimal? discountPercent,
            DateTime expiresAt)
        {
            return new DiscountCode
            {
                Code = code,
                DiscountAmount = discountAmount,
                DiscountPercent = discountPercent,
                MinOrderAmount = 0,
                ExpiresAt = expiresAt,
                MaxUses = 1,
                UsedCount = 0,
                IsActive = true,
                VoucherType = VoucherType.Discount,
                OwnerPhone = phone
            };
        }
    }
}
