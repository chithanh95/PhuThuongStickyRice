using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Messenger
{
    /// <summary>
    /// Mẫu tin nhắn (Zalo/SMS) admin soạn sẵn ở /admin/mau-tin-nhan để gửi
    /// nhanh cho khách (xác nhận đơn, nhắc hẹn...). Nội dung hỗ trợ
    /// placeholder như {TenKhach}, {MaDon}.
    /// </summary>
    public class MessageTemplateDto
    {
        public Guid Id { get; set; }

        /// <summary>Tên gợi nhớ của mẫu tin (hiện trong danh sách chọn).</summary>
        public string Name { get; set; }

        /// <summary>Kênh gửi: "SMS" hoặc "Zalo".</summary>
        public string Channel { get; set; }

        /// <summary>Nội dung mẫu tin, có thể chứa placeholder {TenKhach}, {MaDon}...</summary>
        public string Body { get; set; }
    }

    /// <summary>
    /// Body tạo/sửa mẫu tin nhắn ở trang admin.
    /// </summary>
    public class CreateUpdateMessageTemplateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>Kênh gửi: "SMS" hoặc "Zalo".</summary>
        [Required]
        [StringLength(20)]
        public string Channel { get; set; }

        [Required]
        [StringLength(2000)]
        public string Body { get; set; }
    }
}
