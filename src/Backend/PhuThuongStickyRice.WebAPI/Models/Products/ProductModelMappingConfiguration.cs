using PhuThuongStickyRice.Application.Common.DTOs;
using PhuThuongStickyRice.Application.Products.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace PhuThuongStickyRice.WebAPI.Models.Products
{
    public static class ProductModelMappingConfiguration
    {
        public static ProductModel ToModel(this ProductDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new ProductModel
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Currency = dto.Currency,
                Status = dto.Status,
            };
        }

        public static Paged<ProductModel> ToModels(this Paged<ProductDto> paged)
        {
            if (paged == null)
            {
                return null;
            }

            return new Paged<ProductModel>
            {
                TotalItems = paged.TotalItems,
                Items = paged.Items?.Select(x => x.ToModel()).ToList() ?? new List<ProductModel>()
            };
        }
    }
}
