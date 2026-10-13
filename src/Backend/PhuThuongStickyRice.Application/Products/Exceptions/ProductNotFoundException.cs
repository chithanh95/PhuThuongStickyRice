using System;

namespace PhuThuongStickyRice.Application.Products.Exceptions
{
    public class ProductNotFoundException : Exception
    {
        public ProductNotFoundException(Guid id)
            : base($"Product with id '{id}' was not found.")
        {
        }
    }
}
