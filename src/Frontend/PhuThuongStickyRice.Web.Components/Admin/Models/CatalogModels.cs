using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Admin.Models
{
    /// <summary>
    /// Topping/món ăn kèm cho phép khách thêm vào xôi: tên, phụ thu và thứ tự hiển thị.
    /// </summary>
    public class ToppingModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public decimal ExtraPrice { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Một món trong combo: sản phẩm, giá tại thời điểm thêm combo và số lượng.
    /// </summary>
    public class ComboItemModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal ProductPrice { get; set; }

        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// Combo gồm nhiều món bán với giá trọn gói, rẻ hơn mua lẻ từng món.
    /// </summary>
    public class ComboModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public List<ComboItemModel> Items { get; set; } = new List<ComboItemModel>();
    }

    /// <summary>
    /// Yêu cầu báo giá của khách (đặt tiệc, đặt số lượng lớn) hiển thị trên trang admin:
    /// thông tin khách, ngày sự kiện, số lượng, loại báo giá, trạng thái xử lý
    /// và tiền đặt cọc (nếu có).
    /// </summary>
    public class QuoteRequestAdminModel
    {
        public Guid Id { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public DateTime? EventDate { get; set; }

        public int Quantity { get; set; }

        public string Notes { get; set; }

        public string EventLocation { get; set; }

        public string Budget { get; set; }

        public string QuoteType { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public decimal? DepositAmount { get; set; }

        public string DepositInfo { get; set; }
    }

    /// <summary>
    /// Tùy chọn size/kích cỡ của một sản phẩm (ví dụ: bát nhỏ/bát lớn):
    /// tên, giá bán của size đó và thứ tự hiển thị.
    /// </summary>
    public class ProductSizeOptionModel
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
