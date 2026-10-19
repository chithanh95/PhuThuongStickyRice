using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Orders
{
        /// <summary>
        /// Thông tin người giao hàng (shipper ngoài, quán hợp đồng theo chuyến).
        /// </summary>
        public class ShipperDto
        {
            /// <summary>Id shipper.</summary>
            public Guid Id { get; set; }

            /// <summary>Tên shipper.</summary>
            public string Name { get; set; }

            /// <summary>SĐT liên hệ.</summary>
            public string Phone { get; set; }

            /// <summary>False = tạm ngưng gọi shipper này.</summary>
            public bool IsActive { get; set; }

            /// <summary>Thời điểm tạo bản ghi.</summary>
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>Input admin tạo shipper mới.</summary>
        public class CreateShipperDto
        {
            /// <summary>Tên shipper (bắt buộc).</summary>
            [Required]
            [StringLength(100)]
            public string Name { get; set; }

            /// <summary>SĐT liên hệ (tùy chọn).</summary>
            [StringLength(30)]
            public string Phone { get; set; }
        }

        /// <summary>
        /// Input admin cập nhật shipper: giống lúc tạo, thêm cờ bật/tắt
        /// để ngưng gọi shipper này mà không xóa dữ liệu.
        /// </summary>
        public class UpdateShipperDto : CreateShipperDto
        {
            /// <summary>True = đang hợp tác, False = tạm ngưng.</summary>
            public bool IsActive { get; set; } = true;
        }
}
