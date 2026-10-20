using PhuThuongStickyRice.Web.Components.Products.Models;
using PhuThuongStickyRice.Web.Components.Products.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
        public NavigationManager Navigation { get; set; }

        [Inject]
        public ILogger<EditProductPage> Logger { get; set; }

        [Parameter]
        public Guid ProductId { get; set; }

        public ProductModel Product { get; set; } = new ProductModel();

        /// <summary>Item 148: change history ("ai sửa gì, khi nào").</summary>
        protected List<ProductChangeLogModel> _changeLog { get; private set; }

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
        /// Tải sản phẩm cần sửa, rồi tải lịch sử thay đổi (fail-soft riêng).
        /// Chỉ HttpRequestException có StatusCode 404 mới tính là "không tìm
        /// thấy"; mọi lỗi còn lại vào LoadError — trước đây catch trần gộp
        /// tất cả (kể cả lỗi mạng/500) thành "không tìm thấy sản phẩm".
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
                if (Logger != null)
                {
                    Log.LoadProductFailed(Logger, ex, ProductId);
                }
                LoadError = ex.Message;
            }
            if (IsNotFound || LoadError != null)
            {
                return;
            }
            try
            {
                _changeLog = await ProductService.GetChangeLogAsync(ProductId);
            }
            catch (Exception ex)
            {
                if (Logger != null)
                {
                    Log.LoadChangeLogFailed(Logger, ex, ProductId);
                }
                _changeLog = new List<ProductChangeLogModel>();
            }
        }

        protected static string DisplayValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(trống)" : value;
        }

        // CA1848: dùng LoggerMessage source-generated thay vì Logger.LogWarning(...) trực tiếp
        // để tránh boxing/format string khi log level bị tắt.
        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Warning, Message = "Không tải được lịch sử thay đổi của sản phẩm {ProductId}")]
            public static partial void LoadChangeLogFailed(ILogger logger, Exception ex, Guid productId);

            [LoggerMessage(Level = LogLevel.Error, Message = "Không tải được sản phẩm {ProductId} để sửa")]
            public static partial void LoadProductFailed(ILogger logger, Exception ex, Guid productId);
        }
    }
}
