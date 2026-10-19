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

        /// <summary>true khi sản phẩm không tải được (không tồn tại, đã bị xóa hoặc lỗi mạng) — trang hiện thông báo thay vì crash.</summary>
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
            if (IsNotFound)
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
        }
    }
}
