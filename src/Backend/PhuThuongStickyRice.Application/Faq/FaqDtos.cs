using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Faq.DTOs
{
    /// <summary>
    /// DTO đọc câu hỏi thường gặp: trả về cho shop hiển thị và admin quản lý.
    /// Thứ tự hiển thị theo <c>SortOrder</c>, ẩn những item có <c>IsActive</c> = false.
    /// </summary>
    public class FaqItemDto
    {
        public Guid Id { get; set; }

        public string Question { get; set; }

        public string Answer { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO tạo câu hỏi thường gặp mới: câu hỏi/trả lời bắt buộc nhập;
    /// <c>IsActive</c> mặc định true nên câu hỏi mới hiển thị ngay trên shop.
    /// </summary>
    public class CreateFaqItemDto
    {
        [Required]
        [StringLength(500)]
        public string Question { get; set; }

        [Required]
        [StringLength(4000)]
        public string Answer { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
