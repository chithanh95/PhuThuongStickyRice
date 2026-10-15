using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Admin.Models
{
    public class ToppingModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public decimal ExtraPrice { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ComboItemModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal ProductPrice { get; set; }

        public int Quantity { get; set; } = 1;
    }

    public class ComboModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public List<ComboItemModel> Items { get; set; } = new List<ComboItemModel>();
    }

    public class QuoteRequestAdminModel
    {
        public Guid Id { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public DateTime? EventDate { get; set; }

        public int Quantity { get; set; }

        public string Notes { get; set; }

        public string EventLocation { get; set; }

        public string QuoteType { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class DishCandidateModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public long Votes { get; set; }

        public bool IsActive { get; set; } = true;
    }

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
