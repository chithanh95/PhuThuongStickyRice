using System;

namespace PhuThuongStickyRice.Web.Components.Shop.Models
{
    /// <summary>Item 164: Thẻ xôi sáng trả trước.</summary>
    public class PrepaidCardModel
    {
        public Guid Id { get; set; }

        public string CardCode { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public int TotalSuits { get; set; }

        public int UsedSuits { get; set; }

        public int RemainingSuits { get; set; }

        public decimal PaidAmount { get; set; }

        /// <summary>0 = Active (Đang dùng), 1 = UsedUp (Đã dùng hết), 2 = Cancelled (Đã hủy).</summary>
        public int Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public string StatusText => Status switch
        {
            1 => "Đã dùng hết",
            2 => "Đã hủy",
            _ => "Đang dùng"
        };
    }

    public class BuyPrepaidCardRequest
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public int PackageSuits { get; set; }
    }
}
