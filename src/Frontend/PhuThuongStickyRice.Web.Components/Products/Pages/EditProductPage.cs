using PhuThuongStickyRice.Web.Components.Products.Models;
using PhuThuongStickyRice.Web.Components.Products.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Products.Pages
{
    public partial class EditProductPage
    {
        [CascadingParameter]
        Task<AuthenticationState> AuthenticationStateTask { get; set; }

        [Inject]
        public ProductService ProductService { get; set; }

        [Inject]
        public NavigationManager NavManager { get; set; }

        [Inject]
        public ILogger<EditProductPage> Logger { get; set; }

        [Parameter]
        public Guid ProductId { get; set; }

        public ProductModel Product { get; set; } = new ProductModel();

        /// <summary>Item 148: change history ("ai sửa gì, khi nào").</summary>
        protected List<ProductChangeLogModel> _changeLog { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            Product = await ProductService.GetProductByIdAsync(ProductId);
            try
            {
                _changeLog = await ProductService.GetChangeLogAsync(ProductId);
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Không tải được lịch sử thay đổi của sản phẩm {ProductId}", ProductId);
                _changeLog = new List<ProductChangeLogModel>();
            }
        }

        protected static string DisplayValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(trống)" : value;
        }
    }
}
