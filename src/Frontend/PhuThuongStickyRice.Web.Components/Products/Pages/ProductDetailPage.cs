using PhuThuongStickyRice.Web.Components.Products.Models;
using PhuThuongStickyRice.Web.Components.Products.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Products.Pages
{
    public partial class ProductDetailPage
    {
        [Inject]
        public ProductService ProductService { get; set; }

        [Inject]
        public NavigationManager Navigation { get; set; }

        [Inject]
        public ILogger<ProductDetailPage> Logger { get; set; }

        [Parameter]
        public Guid ProductId { get; set; }

        public ProductModel Product { get; set; }

        public bool IsNotFound { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                Product = await ProductService.GetProductByIdAsync(ProductId);
                IsNotFound = Product == null;
            }
            catch
            {
                IsNotFound = true;
            }
        }

    }
}
