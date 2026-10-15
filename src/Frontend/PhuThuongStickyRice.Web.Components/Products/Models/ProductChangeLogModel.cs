using System;

namespace PhuThuongStickyRice.Web.Components.Products.Models
{
    /// <summary>
    /// Item 148: one row of the product change history ("ai sửa gì, khi nào").
    /// </summary>
    public class ProductChangeLogModel
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; }

        /// <summary>Vietnamese field label, e.g. "Giá bán".</summary>
        public string FieldName { get; set; }

        public string OldValue { get; set; }

        public string NewValue { get; set; }
    }

    /// <summary>Minimal wrapper for paged API results.</summary>
    public class PagedResultModel<T>
    {
        public long TotalCount { get; set; }

        public System.Collections.Generic.List<T> Items { get; set; }
            = new System.Collections.Generic.List<T>();
    }
}
