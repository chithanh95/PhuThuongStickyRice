using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Landing.DTOs
{
    /// <summary>
    /// Một tin tức hiển thị ở section "Tin tức" của trang landing.
    /// Nội dung do admin nhập, không chứa dữ liệu nhạy cảm.
    /// </summary>
    public class LandingNewsItemDto
    {
        /// <summary>Tiêu đề tin tức hiển thị trên landing.</summary>
        public string Title { get; set; }

        /// <summary>Nội dung tóm tắt của tin tức.</summary>
        public string Body { get; set; }

        /// <summary>Nguồn bài viết (vd: tên báo hoặc "Quán Xôi Huyền").</summary>
        public string Source { get; set; }
    }

    /// <summary>
    /// Toàn bộ nội dung động của trang landing do admin sửa ở màn hình
    /// /admin/landing: chữ các section, ảnh (lấy từ setting DB, không
    /// hardcode) và cấu hình popup/banner hiển thị trên trang.
    /// </summary>
    public class LandingContentDto
    {
        /// <summary>Tiêu đề lớn ở đầu trang landing (hero section).</summary>
        public string HeroTitle { get; set; }

        /// <summary>Câu slogan/dòng giới thiệu ngắn dưới tiêu đề hero.</summary>
        public string HeroTagline { get; set; }

        /// <summary>Chữ trên nút chính của hero (vd: "Đặt xôi ngay").</summary>
        public string HeroButtonText { get; set; }

        /// <summary>Tiêu đề section câu chuyện thương hiệu.</summary>
        public string StoryTitle { get; set; }

        /// <summary>Nội dung câu chuyện thương hiệu.</summary>
        public string StoryBody { get; set; }

        /// <summary>Tiêu đề section thực đơn trên landing.</summary>
        public string MenuTitle { get; set; }

        /// <summary>Nhãn danh mục món thứ nhất trên landing.</summary>
        public string MenuLabel1 { get; set; }

        /// <summary>Nhãn danh mục món thứ hai trên landing.</summary>
        public string MenuLabel2 { get; set; }

        /// <summary>Nhãn danh mục món thứ ba trên landing.</summary>
        public string MenuLabel3 { get; set; }

        /// <summary>Tiêu đề section tin tức trên landing.</summary>
        public string NewsTitle { get; set; }

        /// <summary>Danh sách tin tức hiển thị ở section tin tức.</summary>
        public List<LandingNewsItemDto> NewsItems { get; set; } = new List<LandingNewsItemDto>();

        /// <summary>Tiêu đề cột giới thiệu ở footer.</summary>
        public string FooterTitle { get; set; }

        /// <summary>Mô tả ngắn về quán ở footer (admin sửa được).</summary>
        public string FooterAboutText { get; set; }

        /// <summary>Dòng copyright ở cuối footer (admin sửa được).</summary>
        public string FooterCopyrightText { get; set; }

        /// <summary>Địa chỉ liên hệ hiển thị ở footer.</summary>
        public string ContactAddress { get; set; }

        /// <summary>Số điện thoại liên hệ hiển thị ở footer.</summary>
        public string ContactPhone { get; set; }

        /// <summary>Email liên hệ hiển thị ở footer.</summary>
        public string ContactEmail { get; set; }

        /// <summary>Chữ trên nút liên hệ ở footer.</summary>
        public string FooterButtonText { get; set; }

        /// <summary>URL ảnh nền hero (lấy từ setting DB, không hardcode).</summary>
        public string HeroImageUrl { get; set; }

        /// <summary>URL ảnh banner quảng cáo trên landing.</summary>
        public string BannerImageUrl { get; set; }

        /// <summary>URL ảnh minh họa món thứ nhất trên landing.</summary>
        public string MenuImage1Url { get; set; }

        /// <summary>URL ảnh minh họa món thứ hai trên landing.</summary>
        public string MenuImage2Url { get; set; }

        /// <summary>URL ảnh minh họa món thứ ba trên landing.</summary>
        public string MenuImage3Url { get; set; }

        /// <summary>URL ảnh nền của footer.</summary>
        public string FooterBackgroundUrl { get; set; }

        /// <summary>YouTube video title shown above the embed.</summary>
        public string VideoTitle { get; set; }

        /// <summary>YouTube watch/share URL; rendered as an embed.</summary>
        public string VideoUrl { get; set; }

        /// <summary>Tiêu đề video đánh giá của khách hiển thị trên landing.</summary>
        public string ReviewVideoTitle { get; set; }

        /// <summary>URL video đánh giá của khách (render dạng embed).</summary>
        public string ReviewVideoUrl { get; set; }

        /// <summary>Google Maps embed URL rendered as an iframe.</summary>
        public string MapEmbedUrl { get; set; }

        /// <summary>Trusted raw HTML snippet (reviews widget). Admin-only.</summary>
        public string ReviewsEmbedHtml { get; set; }

        /// <summary>Promo popup title (once per session when active).</summary>
        public string PopupTitle { get; set; }

        /// <summary>Promo popup body.</summary>
        public string PopupBody { get; set; }

        /// <summary>Promo popup image URL.</summary>
        public string PopupImageUrl { get; set; }

        /// <summary>Show the promo popup on the landing page.</summary>
        public bool PopupActive { get; set; }

        /// <summary>Top flash banner text.</summary>
        public string FlashBannerText { get; set; }

        /// <summary>Show the top flash banner on the landing page.</summary>
        public bool FlashBannerActive { get; set; }

        /// <summary>Show the promo countdown banner on the landing page.</summary>
        public bool PromoCountdownActive { get; set; }

        /// <summary>Promo countdown end time (UTC). Null = no end.</summary>
        public DateTime? PromoCountdownEnd { get; set; }

        /// <summary>Promo countdown banner text.</summary>
        public string PromoCountdownText { get; set; }
    }
}
