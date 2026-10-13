using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Application.Products.DTOs
{
    public static class ProductMappings
    {
        public static ProductDto ToDto(this Product product)
        {
            if (product == null)
            {
                return null;
            }

            return new ProductDto
            {
                Id = product.Id,
                Code = product.Code.Value,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price.Amount,
                Currency = product.Price.Currency,
                Status = product.Status.ToString()
            };
        }
    }
}
