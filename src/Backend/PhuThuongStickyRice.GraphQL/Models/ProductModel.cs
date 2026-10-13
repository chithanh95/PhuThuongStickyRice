using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.GraphQL.Models
{
    public class ProductModel
    {
        public Guid Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; }

        public string Status { get; set; }
    }

    public class PagedModel<T>
    {
        public long TotalItems { get; set; }

        public List<T> Items { get; set; }
    }
}
