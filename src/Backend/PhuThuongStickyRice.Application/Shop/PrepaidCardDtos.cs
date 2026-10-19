using PhuThuongStickyRice.Domain.Entities;
using System;

namespace PhuThuongStickyRice.Application.Shop
{
    public class BuyPrepaidCardDto
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>Package size: only 20 or 30 suits.</summary>
        public int PackageSuits { get; set; }
    }

    public class PrepaidCardDto
    {
        public Guid Id { get; set; }

        public string CardCode { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public int TotalSuits { get; set; }

        public int UsedSuits { get; set; }

        public int RemainingSuits { get; set; }

        public decimal PaidAmount { get; set; }

        public PrepaidCardStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
