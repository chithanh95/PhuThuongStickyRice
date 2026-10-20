using PhuThuongStickyRice.Web.Components.Products.Models;
using PhuThuongStickyRice.Web.Components.Products.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
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

        /// <summary>true chỉ khi API trả 404 — sản phẩm không tồn tại hoặc đã bị xóa.</summary>
        public bool IsNotFound { get; set; }

        /// <summary>
        /// Thông báo lỗi khi tải sản phẩm thất bại vì lý do KHÁC 404 (API 500,
        /// mất mạng...). Có giá trị thì trang hiện alert lỗi + nút thử lại thay
        /// vì báo sai "Không tìm thấy sản phẩm".
        /// </summary>
        public string LoadError { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadProductAsync();
        }

        /// <summary>
        /// Tải chi tiết sản phẩm. Chỉ HttpRequestException có StatusCode 404
        /// mới tính là "không tìm thấy" (ApiClientBase ném lỗi này qua
        /// EnsureSuccessStatusCode); mọi lỗi còn lại vào LoadError để người
        /// dùng thấy lỗi thật và thử lại được — trước đây catch trần gộp tất
        /// cả thành "không tìm thấy".
        /// </summary>
        public async Task LoadProductAsync()
        {
            IsNotFound = false;
            LoadError = null;
            try
            {
                Product = await ProductService.GetProductByIdAsync(ProductId);
                IsNotFound = Product == null;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                IsNotFound = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Không tải được chi tiết sản phẩm {ProductId}", ProductId);
                LoadError = ex.Message;
            }
        }

    }
}
