using PhuThuongStickyRice.GraphQL.DownstreamServices;
using PhuThuongStickyRice.GraphQL.Models;
using GraphQL;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.GraphQL
{
    public class ProductInput
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; }
    }

    public class PhuThuongStickyRiceMutation
    {
        public static Task<ProductModel> CreateProduct([FromServices] IConfiguration configuration, [FromServices] IHttpContextAccessor httpContext, ProductInput product)
        {
            var productService = GetProductService(configuration, httpContext);
            var createdProduct = productService.CreateProduct(new ProductModel
            {
                Code = product.Code,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Currency = product.Currency,
            });
            return createdProduct;
        }

        public static async Task<bool> DeleteProduct([FromServices] IConfiguration configuration, [FromServices] IHttpContextAccessor httpContext, [Id] string id)
        {
            var productService = GetProductService(configuration, httpContext);
            await productService.DeleteProduct(Guid.Parse(id));
            return true;
        }

        private static ProductService GetProductService(IConfiguration configuration, IHttpContextAccessor httpContext)
        {
            var accessToken = httpContext.HttpContext.Request.Headers["Authorization"].ToString();
            var client = new HttpClient();
            client.BaseAddress = new Uri(configuration["ResourceServer:Endpoint"]);
            client.Timeout = new TimeSpan(0, 0, 30);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", accessToken);
            return new ProductService(client);
        }
    }
}
