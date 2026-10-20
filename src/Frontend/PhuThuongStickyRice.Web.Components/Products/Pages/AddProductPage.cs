using PhuThuongStickyRice.Web.Components.Products.Models;
using PhuThuongStickyRice.Web.Components.Products.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Products.Pages
{
    public partial class AddProductPage
    {
        [CascadingParameter]
        Task<AuthenticationState> AuthenticationStateTask { get; set; }

        [Inject]
        public ProductService ProductService { get; set; }

        [Inject]
        public NavigationManager Navigation { get; set; }

        [Inject]
        public ILogger<AddProductPage> Logger { get; set; }

        public ProductModel Product { get; set; }

        protected override void OnInitialized()
        {
            Product = new ProductModel { };
        }
    }
}
