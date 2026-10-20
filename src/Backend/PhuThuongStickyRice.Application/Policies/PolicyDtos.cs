using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Policies.DTOs
{
    /// <summary>
    /// Chi tiết 1 trang chính sách (chính sách giao hàng, đổi trả, bảo mật...):
    /// nội dung hiển thị trên shop và link ở footer.
    /// </summary>
    public class PolicyPageDto
    {
        public Guid Id { get; set; }

        /// <summary>Định danh đường dẫn trang (không đổi sau khi tạo — dùng làm key tra cứu).</summary>
        public string Slug { get; set; }

        /// <summary>Tiêu đề hiển thị của trang chính sách.</summary>
        public string Title { get; set; }

        /// <summary>Nội dung HTML của trang chính sách.</summary>
        public string Body { get; set; }

        /// <summary>Thứ tự hiển thị trong danh sách/footer (số càng nhỏ càng lên trước).</summary>
        public int SortOrder { get; set; }
    }

    /// <summary>Public: chỉ slug + tiêu đề, dùng cho link chính sách ở footer.</summary>
    public class PolicyPageSummaryDto
    {
        public string Slug { get; set; }

        public string Title { get; set; }
    }

    /// <summary>Admin: sửa tiêu đề/nội dung/thứ tự. Slug là bất biến (không cho sửa).</summary>
    public class UpdatePolicyPageDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Body { get; set; }

        public int SortOrder { get; set; }
    }
}
