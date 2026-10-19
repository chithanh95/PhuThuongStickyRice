using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Discounts
{
        /// <summary>
        /// Khuyến mãi "giờ vàng" (Item 91) — hiển thị trên trang admin và được
        /// checkout tự áp khi đang trong khung giờ (theo giờ Việt Nam).
        /// </summary>
        public class PromotionDto
        {
            /// <summary>Id khuyến mãi (khóa chính GUID).</summary>
            public Guid Id { get; set; }

            /// <summary>Tên gợi nhớ của khuyến mãi.</summary>
            public string Name { get; set; }

            /// <summary>% giảm giá áp vào đơn (0.01–100).</summary>
            public decimal DiscountPercent { get; set; }

            /// <summary>Giờ bắt đầu áp dụng (giờ địa phương VN), dạng "HH:mm".</summary>
            public string StartTime { get; set; }

            /// <summary>Giờ kết thúc áp dụng (không bao gồm mốc này, giờ địa phương VN), dạng "HH:mm".</summary>
            public string EndTime { get; set; }

            /// <summary>Ngày trong tuần áp dụng (bit flags PromotionDay: 1=T2 .. 64=CN, 127=cả tuần).</summary>
            public int DaysOfWeekFlags { get; set; }

            /// <summary>Khuyến mãi có đang được áp dụng không.</summary>
            public bool IsActive { get; set; }
        }

        /// <summary>
        /// Input tạo/sửa khuyến mãi "giờ vàng" từ form admin.
        /// Giờ bắt đầu/kết thúc nhập dạng "HH:mm"; ngày trong tuần là bit flags
        /// (1=T2 .. 64=CN, 127=cả tuần, mặc định 127).
        /// </summary>
        public class CreatePromotionDto
        {
            /// <summary>Tên gợi nhớ của khuyến mãi (tối đa 200 ký tự).</summary>
            [Required]
            [StringLength(200)]
            public string Name { get; set; }

            /// <summary>% giảm giá áp vào đơn (0.01–100).</summary>
            [Range(0.01, 100)]
            public decimal DiscountPercent { get; set; }

            /// <summary>Giờ bắt đầu áp dụng, dạng "HH:mm".</summary>
            [Required]
            public string StartTime { get; set; }

            /// <summary>Giờ kết thúc áp dụng (không bao gồm mốc này), dạng "HH:mm".</summary>
            [Required]
            public string EndTime { get; set; }

            /// <summary>Ngày trong tuần áp dụng (bit flags PromotionDay, 1–127; mặc định cả tuần).</summary>
            [Range(1, 127)]
            public int DaysOfWeekFlags { get; set; } = 127;

            /// <summary>Khuyến mãi có bật ngay sau khi lưu không (mặc định bật).</summary>
            public bool IsActive { get; set; } = true;
        }
}
