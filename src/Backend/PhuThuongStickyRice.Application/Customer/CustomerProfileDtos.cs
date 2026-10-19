using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Customer
{
    /// <summary>
    /// Hồ sơ khách hàng hiển thị ở trang tra cứu đơn: khẩu vị (tag/ghi chú),
    /// ngày sinh (kèm quyền đổi và gợi ý) và mã quà sinh nhật năm nay (nếu có).
    /// </summary>
    public class CustomerProfileDto
    {
        public List<string> TasteTags { get; set; } = new List<string>();

        public string TasteNote { get; set; }

        public DateTime? DateOfBirth { get; set; }

        /// <summary>False when DOB was changed within the last year.</summary>
        public bool CanChangeDateOfBirth { get; set; }

        public string DateOfBirthChangeHint { get; set; }

        /// <summary>This year's birthday gift code (tied to the customer's phone).</summary>
        public string BirthdayGiftCode { get; set; }

        public decimal? BirthdayGiftAmount { get; set; }

        public DateTime? BirthdayGiftExpiresAt { get; set; }
    }

    /// <summary>Input khách tự cập nhật hồ sơ: khẩu vị yêu thích và ngày sinh.</summary>
    public class UpdateCustomerProfileDto
    {
        public List<string> TasteTags { get; set; }

        public string TasteNote { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }

    /// <summary>
    /// Khách hàng có sinh nhật sắp tới (màn hình admin): ngày sinh, đã nhận
    /// quà năm nay hay chưa và mã quà đã tặng.
    /// </summary>
    public class BirthdayMemberDto
    {
        public Guid UserId { get; set; }

        public string Phone { get; set; }

        public string UserName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public bool HasGiftThisYear { get; set; }

        public string GiftCode { get; set; }
    }

    /// <summary>Input admin tặng quà sinh nhật: số tiền giảm (VNĐ), mặc định 25.000đ.</summary>
    public class GrantBirthdayGiftDto
    {
        /// <summary>Gift amount in VND. Defaults to 25000.</summary>
        public decimal DiscountAmount { get; set; } = 25000;
    }

    /// <summary>Kết quả sau khi tặng quà sinh nhật: mã giảm giá vừa tạo, số tiền giảm và hạn dùng.</summary>
    public class BirthdayGiftResultDto
    {
        public string Code { get; set; }

        public decimal DiscountAmount { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>Input admin tạo lời nhắc/chăm sóc khách hàng: SĐT, tên khách và nội dung tin nhắn.</summary>
    public class CreateReminderDto
    {
        public string Phone { get; set; }

        public string CustomerName { get; set; }

        public string Message { get; set; }
    }

    /// <summary>
    /// Bản ghi lời nhắc/chăm sóc khách hàng: người gửi, nội dung, kênh gửi
    /// (vd Zalo/SMS) và thời gian tạo — để admin truy vết lịch sử chăm sóc.
    /// </summary>
    public class CustomerReminderDto
    {
        public Guid Id { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerName { get; set; }

        public string Message { get; set; }

        public string SentVia { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
