using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.WebAPI.Models.Products
{
    /// <summary>
    /// Item 146: body of POST api/products/reorder — product ids in
    /// display order (drag &amp; drop on /products).
    /// </summary>
    public class ReorderProductsModel
    {
        public List<Guid> Ids { get; set; } = [];
    }
}
