using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Security
{
        /// <summary>
        /// SĐT bị chặn đặt hàng (item 88 — chống spam/bom hàng): checkout kiểm tra
        /// ở <c>EnsureCheckoutGuardsAsync</c> và từ chối ngay khi SĐT nằm trong
        /// danh sách này. Kèm lý do chặn để staff tra cứu.
        /// </summary>
        public class BlacklistedPhoneDto
        {
            public Guid Id { get; set; }

            public string Phone { get; set; }

            public string Reason { get; set; }

            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// Dữ liệu thêm SĐT vào danh sách chặn: SĐT bắt buộc; lý do tùy chọn
        /// (tối đa 500 ký tự) để staff biết vì sao bị chặn khi tra cứu.
        /// </summary>
        public class CreateBlacklistedPhoneDto
        {
            [Required]
            [StringLength(30)]
            public string Phone { get; set; }

            [StringLength(500)]
            public string Reason { get; set; }
        }
}
