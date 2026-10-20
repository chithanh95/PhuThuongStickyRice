using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Content
{
        /// <summary>
        /// DTO bài viết tin tức (item 93) cho mục "Chuyện nhà xôi" trên landing
        /// page. List admin trả Body rút gọn (excerpt 220 ký tự); form sửa bài
        /// lấy full Body qua <c>GetByIdAsync</c>.
        /// </summary>
        public class ArticleDto
        {
            /// <summary>Id bài viết.</summary>
            public Guid Id { get; set; }

            /// <summary>Tiêu đề bài viết (tối đa 300 ký tự).</summary>
            public string Title { get; set; }

            /// <summary>Nội dung bài viết; ở list admin chỉ là excerpt 220 ký tự.</summary>
            public string Body { get; set; }

            /// <summary>URL ảnh minh họa (có thể null).</summary>
            public string ImageUrl { get; set; }

            /// <summary>Ngày đăng hiển thị (đã quy đổi sang UTC).</summary>
            public DateTime PublishedAt { get; set; }

            /// <summary>True: bài hiển thị trên landing; false: nháp, chỉ admin thấy.</summary>
            public bool IsPublished { get; set; }
        }

        /// <summary>
        /// Dữ liệu tạo/sửa bài viết từ form admin. <c>PublishedAt</c> để trống
        /// thì lúc tạo lấy thời điểm hiện tại; lúc sửa thì ngày đăng cũ được
        /// giữ nguyên (tránh ghi đè về hiện tại mỗi lần sửa tiêu đề).
        /// </summary>
        public class CreateArticleDto
        {
            /// <summary>Tiêu đề bài viết (bắt buộc, tối đa 300 ký tự).</summary>
            [Required]
            [StringLength(300)]
            public string Title { get; set; }

            /// <summary>Nội dung bài viết (bắt buộc, tối đa 10.000 ký tự).</summary>
            [Required]
            [StringLength(10000)]
            public string Body { get; set; }

            /// <summary>URL ảnh minh họa (tùy chọn, tối đa 500 ký tự); trống lưu null.</summary>
            [StringLength(500)]
            public string ImageUrl { get; set; }

            /// <summary>Ngày đăng (tùy chọn); trống thì lấy thời điểm tạo.</summary>
            public DateTime? PublishedAt { get; set; }

            /// <summary>True: xuất bản ngay; false: lưu nháp (mặc định là true).</summary>
            public bool IsPublished { get; set; } = true;
        }
}
