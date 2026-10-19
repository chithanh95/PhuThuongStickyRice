using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Shop
{
        /// <summary>
        /// Một dòng món trong đơn bán tại quầy (POS): món + số lượng + tùy chọn
        /// size và topping kèm theo.
        /// </summary>
        public class PosOrderLineDto
        {
            public Guid ProductId { get; set; }
            public int Quantity { get; set; }
            public Guid? SizeOptionId { get; set; }
            public List<Guid> ToppingIds { get; set; } = new List<Guid>();
        }

        /// <summary>
        /// Dữ liệu đầu vào khi nhân viên tạo đơn tại quầy (POS).
        /// Khác checkout online ở chỗ không cần địa chỉ giao hàng.
        /// </summary>
        public class CreatePosOrderInput
        {
            [StringLength(200)]
            public string CustomerName { get; set; }

            [StringLength(30)]
            public string CustomerPhone { get; set; }

            [Required]
            [MinLength(1)]
            public List<PosOrderLineDto> Lines { get; set; } = new List<PosOrderLineDto>();

            [StringLength(500)]
            public string Note { get; set; }
        }
}
