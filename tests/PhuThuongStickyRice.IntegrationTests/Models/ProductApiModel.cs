using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.IntegrationTests.Models
{
    /// <summary>
    /// Mirrors the product shape exposed by the WebAPI/GraphQL. Kept local to the
    /// test project so tests don't depend on domain entities.
    /// </summary>
    public class ProductApiModel
    {
        public Guid Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; }

        public string Status { get; set; }
    }

    public class PagedApiModel<T>
    {
        public long TotalItems { get; set; }

        public List<T> Items { get; set; }
    }
}
