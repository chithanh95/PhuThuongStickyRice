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
    public partial class ProductListPage
    {
        [CascadingParameter]
        Task<AuthenticationState> AuthenticationStateTask { get; set; }

        [Inject]
        public ProductService ProductService { get; set; }

        [Inject]
        public NavigationManager Navigation { get; set; }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Inject]
        public ILogger<ProductListPage> Logger { get; set; }

        [Inject]
        public PermissionService PermissionService { get; set; }

        protected ConfirmDialog DeleteDialog { get; set; }

        public List<ProductModel> Products { get; set; }

        /// <summary>Tìm kiếm + phân trang (kiểu Lexa).</summary>
        protected string SearchText { get; set; } = "";
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;

        protected IEnumerable<ProductModel> FilteredProducts =>
            (string.IsNullOrWhiteSpace(SearchText)
                ? (Products ?? Enumerable.Empty<ProductModel>())
                : Products.Where(p =>
                    p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (p.Code != null && p.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt ?? DateTime.MinValue);

        protected int TotalPages =>
            Math.Max(1, (int)Math.Ceiling(FilteredProducts.Count() / (double)PageSize));

        protected IEnumerable<ProductModel> PagedProducts =>
            FilteredProducts.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        protected void OnSearchChanged(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString() ?? "";
            CurrentPage = 1;
        }

        protected void NavigateToPage(int page)
        {
            if (page >= 1 && page <= TotalPages)
                CurrentPage = page;
        }

        /// <summary>Item 155: fine-grained permission keys of the current user.</summary>
        public HashSet<string> Permissions { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        public bool HasPermission(string key) => Permissions.Contains(key);

        public ProductModel DeletingProduct { get; private set; }

        public string AvailabilityToggleError { get; private set; }

        public string DeleteProductError { get; private set; }

        /// <summary>One-click sold-out toggle (Active &lt;-&gt; Inactive).</summary>
        protected async Task ToggleAvailability(ProductModel product)
        {
            AvailabilityToggleError = null;
            try
            {
                var updated = await ProductService.ToggleAvailabilityAsync(product.Id);
                product.Status = updated.Status;
            }
            catch (System.Exception)
            {
                AvailabilityToggleError = $"Không đổi được trạng thái của '{product.Name}'. Vui lòng thử lại.";
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
            // perf-hunt 2026-10-19 (run 00:05): 2 loader độc lập (permissions +
            // products ghi 2 field khác nhau) → chạy song song bằng Task.WhenAll
            // thay vì tuần tự, cắt ~1 HTTP round-trip khi mở trang /products.
            // Permissions giữ fail-soft như cũ (lỗi → tập rỗng).
            var permissionsTask = LoadPermissionsAsync();
            var productsTask = ProductService.GetProductsAsync();
            await Task.WhenAll(permissionsTask, productsTask);
            Products = await productsTask;
        }

        /// <summary>
        /// Tải quyền của user hiện tại; lỗi thì trả tập rỗng (fail-soft) để
        /// trang /products vẫn mở được, các nút nhạy cảm bị ẩn như trước.
        /// </summary>
        private async Task LoadPermissionsAsync()
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
        }

        protected void QuickAddProduct()
        {
            Navigation.NavigateTo("/products/add");
        }

        protected async Task EditProduct(ProductModel product)
        {
            var authenticationState = await AuthenticationStateTask;
            Navigation.NavigateTo($"/products/edit/{product.Id}");
        }

        protected void DeleteProduct(ProductModel product)
        {
            DeletingProduct = product;
            DeleteDialog.Show();
        }

        /// <summary>
        /// Xóa sản phẩm đã chọn sau khi admin xác nhận ở hộp thoại.
        /// Tải lại danh sách khi xóa thành công, hiện thông báo lỗi khi thất bại.
        /// </summary>
        public async Task ConfirmedDeleteProduct()
        {
            try
            {
                await ProductService.DeleteProductAsync(DeletingProduct.Id);
                DeleteDialog.Close();
                Products = await ProductService.GetProductsAsync();
                StateHasChanged();
            }
            catch (System.Exception ex)
            {
                Log.DeleteProductFailed(Logger, ex);
                DeleteDialog.Close();
                DeleteProductError = $"Không xóa được '{DeletingProduct?.Name}'. Vui lòng thử lại.";
                StateHasChanged();
            }
        }

        protected async Task ExportAsPdf()
        {
            var token = await ProductService.GetAccessToken();
            await JSRuntime.InvokeVoidAsync("interop.downloadFile", ProductService.GetExportPdfUrl(), token, "Products.pdf");
        }

        protected async Task ExportAsCsv()
        {
            var token = await ProductService.GetAccessToken();
            await JSRuntime.InvokeVoidAsync("interop.downloadFile", ProductService.GetExportCsvUrl(), token, "Products.csv");
        }

        // CA1848: dùng LoggerMessage source-generated thay vì Logger.LogError(...) trực tiếp
        // để tránh boxing/format string khi log level bị tắt.
        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Error, Message = "Xóa sản phẩm thất bại.")]
            public static partial void DeleteProductFailed(ILogger logger, Exception ex);
        }
    }
}
