using System;

namespace PhuThuongStickyRice.Application.Products.Exceptions
{
    public class DuplicateProductCodeException : Exception
    {
        public DuplicateProductCodeException(string code)
            : base($"A product with code '{code}' already exists.")
        {
        }
    }
}
