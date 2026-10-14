using PhuThuongStickyRice.CrossCuttingConcerns.Exceptions;
using System;

namespace PhuThuongStickyRice.Application.Products.Exceptions
{
    public class ProductNotFoundException : NotFoundException
    {
        public ProductNotFoundException(Guid id)
            : base($"Product with id '{id}' was not found.")
        {
        }
    }
}
