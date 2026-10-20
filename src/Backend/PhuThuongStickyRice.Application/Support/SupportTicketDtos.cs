using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Support.DTOs
{
    /// <summary>
    /// DTO ticket hỗ trợ khách hàng: khách gửi yêu cầu (cho phép ẩn danh,
    /// SĐT là kênh liên hệ chính); admin xem danh sách và trả lời.
    /// </summary>
    public class SupportTicketDto
    {
        /// <summary>Id ticket.</summary>
        public Guid Id { get; set; }

        /// <summary>Tên người gửi.</summary>
        public string Name { get; set; }

        /// <summary>SĐT người gửi — kênh liên hệ lại duy nhất.</summary>
        public string Phone { get; set; }

        /// <summary>Mã đơn hàng liên quan (tùy chọn, có thể trống).</summary>
        public string OrderNumber { get; set; }

        /// <summary>Nội dung yêu cầu hỗ trợ của khách.</summary>
        public string Message { get; set; }

        /// <summary>Trạng thái ticket: "New", "InProgress" hoặc "Done".</summary>
        public string Status { get; set; }

        /// <summary>Lời nhắn trả lời của admin (null khi chưa trả lời).</summary>
        public string ReplyNote { get; set; }

        /// <summary>Thời điểm khách gửi ticket.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Thời điểm admin trả lời (null khi chưa trả lời).</summary>
        public DateTime? RepliedAt { get; set; }
    }

    /// <summary>
    /// Dữ liệu khách tạo ticket hỗ trợ — cho phép ẩn danh nên chỉ cần tên,
    /// SĐT và nội dung, không cần đăng nhập.
    /// </summary>
    public class CreateSupportTicketDto
    {
        /// <summary>Tên người gửi (bắt buộc, tối đa 200 ký tự).</summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        /// <summary>SĐT người gửi (bắt buộc, tối đa 30 ký tự) — kênh duy nhất để quán liên hệ lại.</summary>
        [Required]
        [StringLength(30)]
        public string Phone { get; set; }

        /// <summary>Mã đơn hàng liên quan (tùy chọn, tối đa 30 ký tự).</summary>
        [StringLength(30)]
        public string OrderNumber { get; set; }

        /// <summary>Nội dung cần hỗ trợ (bắt buộc, tối đa 2.000 ký tự).</summary>
        [Required]
        [StringLength(2000)]
        public string Message { get; set; }
    }

    /// <summary>
    /// Dữ liệu admin trả lời ticket. Ticket đã trả lời không được quay về
    /// trạng thái "New" — giữ lịch sử xử lý đúng thứ tự.
    /// </summary>
    public class ReplySupportTicketDto
    {
        /// <summary>Lời nhắn trả lời của admin (bắt buộc, tối đa 2.000 ký tự).</summary>
        [Required]
        [StringLength(2000)]
        public string ReplyNote { get; set; }

        /// <summary>"InProgress" hoặc "Done" (không được trả về "New").</summary>
        [Required]
        public string Status { get; set; }
    }
}
