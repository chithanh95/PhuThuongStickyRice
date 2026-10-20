using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Recurring daily subscription: every morning the subscription worker
    /// creates a Pending COD order for each active subscription due today.
    /// The product/price is snapshotted at subscription time.
    /// </summary>
    public class Subscription : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 30;
        public const int NameMaxLength = 200;
        public const int ProductNameMaxLength = 300;
        public const int SizeNameMaxLength = 100;
        public const int ToppingNamesMaxLength = 500;
        public const int NotesMaxLength = 1000;

        public string Phone { get; set; }

        public string CustomerName { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public string SizeName { get; set; }

        public string ToppingNames { get; set; }

        /// <summary>Snapshotted unit price in VND (incl. size/toppings).</summary>
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public const int AddressMaxLength = 500;

        /// <summary>Delivery address for the auto-created orders.</summary>
        public string DeliveryAddress { get; set; }

        /// <summary>First delivery date (date only, UTC).</summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// true = gói đang hoạt động, worker sinh đơn COD tự động mỗi ngày.
        /// Gói đăng ký từ kênh công khai luôn tạo với false (chờ admin duyệt
        /// và kích hoạt) để tránh đăng ký hộ SĐT người khác rồi tự sinh đơn.
        /// </summary>
        public bool Active { get; set; }

        public string Notes { get; set; }

        /// <summary>Last date an order was auto-created (date only, UTC).</summary>
        public DateTime? LastOrderDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
