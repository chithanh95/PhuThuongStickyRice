using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Tài khoản tích điểm loyalty, định danh bằng SĐT. Theo dõi điểm tích
    /// được từ đơn hoàn thành, mã giới thiệu, voucher sinh nhật và tổng
    /// chi tiêu trọn đời (để xếp hạng thành viên Đồng / Bạc / Vàng).
    /// </summary>
    public class LoyaltyAccount : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Độ dài tối đa cột SĐT trong DB.</summary>
        public const int PhoneMaxLength = 30;
        /// <summary>Độ dài tối đa cột mã giới thiệu trong DB.</summary>
        public const int ReferralCodeMaxLength = 20;

        /// <summary>SĐT chủ tài khoản (chỉ số), duy nhất trong hệ thống.</summary>
        public string Phone { get; set; }

        /// <summary>Số điểm còn dùng được (điểm khả dụng để đổi).</summary>
        public int Points { get; set; }

        /// <summary>Tổng điểm từng tích lũy được (phục vụ báo cáo).</summary>
        public long TotalEarned { get; set; }

        /// <summary>
        /// Tổng chi tiêu trọn đời trên các đơn đã hoàn thành (không tính
        /// đơn hủy), VND. Là căn cứ xếp hạng thành viên (Đồng / Bạc / Vàng).
        /// </summary>
        public decimal TotalSpent { get; set; }

        /// <summary>Mã giới thiệu duy nhất, dạng "PT-AB12CD" — khách nhập mã này khi đặt đơn đầu tiên.</summary>
        public string ReferralCode { get; set; }

        /// <summary>Ngày sinh (không bắt buộc) — worker sinh nhật dùng để tự phát voucher.</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>Thời điểm tạo tài khoản (UTC).</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Thời điểm tài khoản được cập nhật lần cuối (UTC).</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
