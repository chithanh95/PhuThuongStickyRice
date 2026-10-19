using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Discounts
{
        /// <summary>
        /// Một nhóm đối tượng của chiến dịch phát voucher hàng loạt,
        /// kèm số lượng người nhận ước tính để admin chọn trước khi phát hành.
        /// </summary>
        public class VoucherCampaignAudienceDto
        {
            /// <summary>Khóa nhóm: "inactive30" | "tier" | "all".</summary>
            public string Key { get; set; }

            /// <summary>Tên hiển thị của nhóm (vd "Không mua 30+ ngày").</summary>
            public string Name { get; set; }

            /// <summary>Số lượng người nhận ước tính trong nhóm.</summary>
            public int RecipientCount { get; set; }
        }

        /// <summary>
        /// Input admin phát hành voucher hàng loạt: chọn đối tượng, mức giảm,
        /// tiền tố mã và số ngày hiệu lực. Mỗi SĐT nhận đúng 1 mã dùng 1 lần
        /// gắn riêng SĐT đó (không dùng ké được).
        /// </summary>
        public class CreateVoucherCampaignInput
        {
            /// <summary>"inactive30", "tier", or "all".</summary>
            [Required]
            public string Audience { get; set; }

            /// <summary>Only for audience "tier": "Bạc" or "Vàng".</summary>
            public string Tier { get; set; }

            /// <summary>Percent discount, or fixed amount when DiscountAmount is set.</summary>
            [Range(1, 100)]
            public decimal DiscountPercent { get; set; }

            /// <summary>Fixed VND amount (used instead of percent when > 0).</summary>
            [Range(0, 10000000)]
            public decimal DiscountAmount { get; set; }

            /// <summary>Days until expiry. Default 30.</summary>
            [Range(1, 365)]
            public int ValidDays { get; set; } = 30;

            /// <summary>
            /// Tiền tố mã voucher (chỉ giữ chữ/số, viết hoa). Trống hoặc toàn ký tự
            /// đặc biệt thì dùng mặc định "VC".
            /// </summary>
            [StringLength(50)]
            public string Prefix { get; set; } = "VC";
        }

        /// <summary>
        /// Một cặp SĐT → mã voucher đã sinh thành công, để admin copy sang
        /// công cụ gửi tin (Zalo OA / SMS) của quán.
        /// </summary>
        public class GeneratedVoucherDto
        {
            /// <summary>SĐT nhận voucher (mã chỉ SĐT này dùng được).</summary>
            public string Phone { get; set; }

            /// <summary>Mã voucher đã sinh (dùng 1 lần).</summary>
            public string Code { get; set; }
        }
}
