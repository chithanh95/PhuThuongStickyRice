using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Loyalty
{
        /// <summary>
        /// Quy tắc thưởng tự động (Item 92) — hiển thị trên trang admin:
        /// tên, ngưỡng tiền đơn tối thiểu và loại phần thưởng áp dụng.
        /// </summary>
        public class OrderRewardRuleDto
        {
            /// <summary>Id quy tắc (khóa chính GUID).</summary>
            public Guid Id { get; set; }

            /// <summary>Tên gợi nhớ của quy tắc (vd: "Đơn 200k tặng mã 5%").</summary>
            public string Name { get; set; }

            /// <summary>Ngưỡng tiền đơn tối thiểu (VNĐ) để được áp thưởng.</summary>
            public decimal MinAmount { get; set; }

            /// <summary>"FreeTopping" or "DiscountCode".</summary>
            public string RewardType { get; set; }

            /// <summary>Tên topping tặng kèm — chỉ dùng khi RewardType = FreeTopping.</summary>
            public string ToppingName { get; set; }

            /// <summary>% giảm của mã sinh tặng — chỉ dùng khi RewardType = DiscountCode.</summary>
            public decimal? DiscountPercent { get; set; }

            /// <summary>Quy tắc có đang được áp dụng cho đơn mới hoàn tất không.</summary>
            public bool IsActive { get; set; }
        }

        /// <summary>
        /// Input tạo/sửa quy tắc thưởng tự động từ form admin.
        /// Trường bắt buộc khác nhau theo loại: FreeTopping cần ToppingName,
        /// DiscountCode cần DiscountPercent (được validate ở service).
        /// </summary>
        public class CreateOrderRewardRuleDto
        {
            /// <summary>Tên gợi nhớ của quy tắc (tối đa 200 ký tự).</summary>
            [Required]
            [StringLength(200)]
            public string Name { get; set; }

            /// <summary>Ngưỡng tiền đơn tối thiểu (VNĐ), không âm.</summary>
            [Range(0, double.MaxValue)]
            public decimal MinAmount { get; set; }

            /// <summary>Loại thưởng: "FreeTopping" hoặc "DiscountCode".</summary>
            [Required]
            public string RewardType { get; set; }

            /// <summary>Tên topping tặng kèm (bắt buộc khi RewardType = FreeTopping).</summary>
            [StringLength(200)]
            public string ToppingName { get; set; }

            /// <summary>% giảm của mã tặng (bắt buộc, 0.01–100, khi RewardType = DiscountCode).</summary>
            [Range(0.01, 100)]
            public decimal? DiscountPercent { get; set; }

            /// <summary>Quy tắc có bật ngay sau khi lưu không (mặc định bật).</summary>
            public bool IsActive { get; set; } = true;
        }
}
