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
        /// <summary>
        /// Họ tên khách nhập khi đăng ký tài khoản. User entity không có cột
        /// họ tên riêng nên tên được lưu dạng claim "fullName" trên bảng
        /// UserClaims có sẵn (không cần migration) — xem
        /// CustomerProfileAppService.SetFullNameAsync. Checkout dùng tên này
        /// để tự điền ô họ tên cho khách đã đăng nhập. Tài khoản cũ chưa có
        /// claim: server lấy tên từ đơn hàng gần nhất của SĐT tài khoản
        /// (GetLatestOrderNameAsync) để vẫn hiển thị được tên.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>SĐT của tài khoản (User.PhoneNumber) — checkout tự điền ô SĐT.</summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Email của tài khoản (User.Email) — checkout tự điền ô email cho
        /// khách đã đăng nhập (bổ sung 2026-10-24: trước đây hồ sơ không trả
        /// email nên khách đăng nhập vẫn phải gõ lại email ở checkout).
        /// </summary>
        public string Email { get; set; }

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

    /// <summary>Input khách tự cập nhật hồ sơ: họ tên, khẩu vị yêu thích và ngày sinh.</summary>
    public class UpdateCustomerProfileDto
    {
        /// <summary>
        /// Họ tên (lưu dạng claim "fullName" qua SetFullNameAsync). Trang
        /// tài khoản không còn cho sửa tên (user chốt 2026-10-24: họ tên
        /// chỉ hiển thị, load từ tài khoản) nên FE gửi null; trường này
        /// giữ cho tương thích API. null = giữ nguyên tên hiện tại.
        /// </summary>
        public string FullName { get; set; }

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
