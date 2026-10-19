using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Subscriptions.DTOs
{
    /// <summary>
    /// Gói đăng ký mua xôi theo kỳ của khách (vd: gói tuần) — bản đọc trả về
    /// cho shop và admin: thông tin món, số lượng, giá đơn vị, địa chỉ giao và
    /// trạng thái hoạt động của gói.
    /// </summary>
    public class SubscriptionDto
    {
        public Guid Id { get; set; }

        /// <summary>SĐT chủ gói — định danh khách để tạo đơn hàng ngày.</summary>
        public string Phone { get; set; }

        public string CustomerName { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        /// <summary>Tên size đã chọn (vd: nhỏ/vừa/lớn); null khi món không có size.</summary>
        public string SizeName { get; set; }

        /// <summary>Tên các topping đã chọn, nối chuỗi để hiển thị.</summary>
        public string ToppingNames { get; set; }

        /// <summary>Đơn giá 1 phần tại thời điểm tạo gói (đ) — chốt giá, không đổi theo giá menu sau này.</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Số phần mỗi ngày giao cho gói này.</summary>
        public int Quantity { get; set; }

        public string DeliveryAddress { get; set; }

        /// <summary>Ngày bắt đầu giao gói.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>True = gói đang chạy (sinh đơn hàng ngày); false = đã tạm dừng/hủy.</summary>
        public bool Active { get; set; }

        public string Notes { get; set; }

        /// <summary>Ngày đơn gần nhất được sinh từ gói; null khi chưa sinh đơn nào.</summary>
        public DateTime? LastOrderDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Dữ liệu khách nhập khi đăng ký gói mua xôi theo kỳ mới — admin dùng để
    /// tạo gói và bắt đầu sinh đơn hàng ngày từ <see cref="StartDate"/>.
    /// </summary>
    public class CreateSubscriptionDto
    {
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(30)]
        public string Phone { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        public Guid? SizeOptionId { get; set; }

        public List<Guid> ToppingIds { get; set; }
            = new List<Guid>();

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        [Required]
        [StringLength(500)]
        public string DeliveryAddress { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }
    }
}
