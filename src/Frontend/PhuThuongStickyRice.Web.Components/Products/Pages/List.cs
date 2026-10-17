using PhuThuongStickyRice.Web.Components.Core.Components;
using PhuThuongStickyRice.Web.Components.Products.Models;
using PhuThuongStickyRice.Web.Components.Products.Services;
using PhuThuongStickyRice.Web.Components.Admin.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Products.Pages
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

        [Inject]
        public PermissionService PermissionService { get; set; }

        protected ConfirmDialog DeleteDialog { get; set; }

        public List<ProductModel> Products { get; set; }

        /// <summary>Item 155: fine-grained permission keys of the current user.</summary>
        public HashSet<string> Permissions { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        public bool HasPermission(string key) => Permissions.Contains(key);

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
            // Item 155: fine-grained permissions gate the sensitive buttons.
            try
            {
                Permissions = await PermissionService.GetMyPermissionsAsync();
            }
            catch
            {
                Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

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
