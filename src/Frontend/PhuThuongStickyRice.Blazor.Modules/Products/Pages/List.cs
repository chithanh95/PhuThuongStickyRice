using PhuThuongStickyRice.Blazor.Modules.Core.Components;
using PhuThuongStickyRice.Blazor.Modules.Products.Models;
using PhuThuongStickyRice.Blazor.Modules.Products.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Products.Pages
{
    public partial class List
    {
        [CascadingParameter]
        Task<AuthenticationState> AuthenticationStateTask { get; set; }

        [Inject]
        public ProductService ProductService { get; set; }

        [Inject]
        public NavigationManager NavManager { get; set; }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Inject]
        public ILogger<List> Logger { get; set; }

        protected ConfirmDialog DeleteDialog { get; set; }

        public List<ProductModel> Products { get; set; }

        public ProductModel DeletingProduct { get; private set; }

        public string ToggleError { get; private set; }

        /// <summary>One-click sold-out toggle (Active &lt;-&gt; Inactive).</summary>
        protected async Task ToggleAvailability(ProductModel product)
        {
            ToggleError = null;
            try
            {
                var updated = await ProductService.ToggleAvailabilityAsync(product.Id);
                product.Status = updated.Status;
            }
            catch (System.Exception)
            {
                ToggleError = $"Không đổi được trạng thái của '{product.Name}'. Vui lòng thử lại.";
            }
        }

        /// <summary>
        /// True when the product has a seasonal window and today is outside it.
        /// The public shop hides these; the admin list shows a "Hết mùa" badge.
        /// </summary>
        protected static bool IsOutOfSeason(ProductModel product)
        {
            var today = System.DateTime.UtcNow.Date;
            if (product.AvailableFrom.HasValue && today < product.AvailableFrom.Value.Date)
            {
                return true;
            }

            if (product.AvailableTo.HasValue && today > product.AvailableTo.Value.Date)
            {
                return true;
            }

            return false;
        }

        protected override async Task OnInitializedAsync()
        {
            Products = await ProductService.GetProductsAsync();
        }

        protected void QuickAddProduct()
        {
            NavManager.NavigateTo("/products/add");
        }

        protected async Task EditProduct(ProductModel product)
        {
            var authenticationState = await AuthenticationStateTask;
            NavManager.NavigateTo($"/products/edit/{product.Id}");
        }

        protected void DeleteProduct(ProductModel product)
        {
            DeletingProduct = product;
            DeleteDialog.Show();
        }

        public async void ConfirmedDeleteProduct()
        {
            await ProductService.DeleteProductAsync(DeletingProduct.Id);
            DeleteDialog.Close();
            Products = await ProductService.GetProductsAsync();
            StateHasChanged();
        }

        protected async Task ExportAsPdf()
        {
            var token = await ProductService.GetAccessToken();
            await JSRuntime.Log(token);
            await JSRuntime.InvokeVoidAsync("interop.downloadFile", ProductService.GetExportPdfUrl(), token, "Products.pdf");
        }

        protected async Task ExportAsCsv()
        {
            var token = await ProductService.GetAccessToken();
            await JSRuntime.Log(token);
            await JSRuntime.InvokeVoidAsync("interop.downloadFile", ProductService.GetExportCsvUrl(), token, "Products.csv");
        }
    }
}
