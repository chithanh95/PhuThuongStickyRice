using PhuThuongStickyRice.Domain.Entities;
using System;

namespace PhuThuongStickyRice.Application.Shop
{
    /// <summary>
    /// Input khi khách mua gói thẻ trả trước: họ tên, SĐT và gói suất (chỉ
    /// chấp nhận gói 20 hoặc 30 suất — các gói khác service ném lỗi nghiệp vụ).
    /// </summary>
    public class BuyPrepaidCardDto
    {
        /// <summary>Họ tên khách mua thẻ (bắt buộc, để nhân viên đối chiếu khi nhận suất).</summary>
        public string CustomerName { get; set; }

        /// <summary>SĐT khách (bắt buộc, đúng định dạng SĐT VN 10 số) — khóa để tra cứu thẻ.</summary>
        public string CustomerPhone { get; set; }

        /// <summary>Package size: only 20 or 30 suits.</summary>
        public int PackageSuits { get; set; }
    }

    /// <summary>
    /// Thông tin thẻ xôi sáng trả trước trả về cho shop và admin: mã thẻ, gói
    /// suất đã mua, suất đã dùng/còn lại, số tiền đã trả và trạng thái thẻ.
    /// </summary>
    public class PrepaidCardDto
    {
        /// <summary>Id thẻ (khóa chính GUID).</summary>
        public Guid Id { get; set; }

        /// <summary>Mã thẻ duy nhất (vd TX482193) — khách đưa mã này để nhận suất.</summary>
        public string CardCode { get; set; }

        /// <summary>Họ tên khách mua thẻ.</summary>
        public string CustomerName { get; set; }

        /// <summary>SĐT đã đăng ký thẻ (dùng để tra cứu thẻ của mình).</summary>
        public string CustomerPhone { get; set; }

        /// <summary>Tổng số suất của gói (20 hoặc 30).</summary>
        public int TotalSuits { get; set; }

        /// <summary>Số suất đã dùng.</summary>
        public int UsedSuits { get; set; }

        /// <summary>Số suất còn lại (= TotalSuits - UsedSuits).</summary>
        public int RemainingSuits { get; set; }

        /// <summary>Số tiền (VNĐ) khách đã trả khi mua gói (đã chiết khấu theo gói).</summary>
        public decimal PaidAmount { get; set; }

        /// <summary>Trạng thái thẻ: đang dùng / đã dùng hết / đã hủy.</summary>
        public PrepaidCardStatus Status { get; set; }

        /// <summary>Thời điểm tạo thẻ (UTC).</summary>
        public DateTime CreatedAt { get; set; }
    }
}
