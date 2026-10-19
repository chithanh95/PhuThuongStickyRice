using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using CungReminderModel = PhuThuongStickyRice.Web.Components.Shop.Models.CungReminderModel;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Client API cho admin quản trị danh mục: topping, combo, yêu cầu báo
    /// giá, đăng ký nhắc cúng và tùy chọn cỡ món (product size options).
    /// </summary>
    public class CatalogAdminService : ApiClientBase
    {
        public CatalogAdminService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        // Toppings.

        /// <summary>Lấy danh sách topping.</summary>
        public async Task<List<ToppingModel>> GetToppingsAsync()
        {
            return await GetAsync<List<ToppingModel>>("api/toppings");
        }

        /// <summary>Tạo topping mới.</summary>
        /// <param name="model">Thông tin topping cần tạo.</param>
        public async Task<ToppingModel> CreateToppingAsync(ToppingModel model)
        {
            return await PostAsync<ToppingModel>("api/toppings", model);
        }

        /// <summary>Cập nhật topping theo Id.</summary>
        /// <param name="id">Id topping.</param>
        /// <param name="model">Thông tin topping mới.</param>
        public async Task<ToppingModel> UpdateToppingAsync(Guid id, ToppingModel model)
        {
            return await PutAsync<ToppingModel>($"api/toppings/{id}", model);
        }

        /// <summary>Xóa topping theo Id.</summary>
        /// <param name="id">Id topping cần xóa.</param>
        public async Task DeleteToppingAsync(Guid id)
        {
            await DeleteAsync($"api/toppings/{id}");
        }

        // Combos.

        /// <summary>Lấy danh sách combo.</summary>
        public async Task<List<ComboModel>> GetCombosAsync()
        {
            return await GetAsync<List<ComboModel>>("api/combos");
        }

        /// <summary>Tạo combo mới.</summary>
        /// <param name="model">Thông tin combo cần tạo.</param>
        public async Task<ComboModel> CreateComboAsync(ComboModel model)
        {
            return await PostAsync<ComboModel>("api/combos", model);
        }

        /// <summary>Cập nhật combo theo Id.</summary>
        /// <param name="id">Id combo.</param>
        /// <param name="model">Thông tin combo mới.</param>
        public async Task<ComboModel> UpdateComboAsync(Guid id, ComboModel model)
        {
            return await PutAsync<ComboModel>($"api/combos/{id}", model);
        }

        /// <summary>Xóa combo theo Id.</summary>
        /// <param name="id">Id combo cần xóa.</param>
        public async Task DeleteComboAsync(Guid id)
        {
            await DeleteAsync($"api/combos/{id}");
        }

        // Quote requests.

        /// <summary>Lấy 100 yêu cầu báo giá mới nhất; truyền status để lọc theo trạng thái.</summary>
        /// <param name="status">Trạng thái cần lọc (null = tất cả).</param>
        public async Task<List<QuoteRequestAdminModel>> GetQuotesAsync(string status = null)
        {
            var url = "api/quotes?page=1&pageSize=100"
                + (string.IsNullOrWhiteSpace(status) ? "" : $"&status={Uri.EscapeDataString(status)}");
            var paged = await GetAsync<PagedResult<QuoteRequestAdminModel>>(url);
            return paged?.Items ?? new List<QuoteRequestAdminModel>();
        }

        /// <summary>Đổi trạng thái yêu cầu báo giá (duyệt/từ chối...).</summary>
        /// <param name="id">Id yêu cầu báo giá.</param>
        /// <param name="status">Trạng thái mới.</param>
        public async Task<QuoteRequestAdminModel> UpdateQuoteStatusAsync(Guid id, string status)
        {
            return await PutAsync<QuoteRequestAdminModel>($"api/quotes/{id}/status", new { Status = status });
        }

        /// <summary>Item 161: admin ghi nhận tiền cọc khi duyệt báo giá.</summary>
        /// <param name="id">Id yêu cầu báo giá.</param>
        /// <param name="depositAmount">Số tiền cọc (₫).</param>
        /// <param name="depositInfo">Ghi chú cọc (ngân hàng, mã giao dịch...).</param>
                public async Task<QuoteRequestAdminModel> SetQuoteDepositAsync(Guid id, decimal depositAmount, string depositInfo)
        {
            return await PutAsync<QuoteRequestAdminModel>($"api/quotes/{id}/deposit",
                new { DepositAmount = depositAmount, DepositInfo = depositInfo });
        }

        /// <summary>Item 161: lấy thông tin tài khoản ngân hàng để sinh QR cọc VietQR.</summary>
                public async Task<PhuThuongStickyRice.Web.Components.Shop.Models.BankTransferInfoModel> GetBankTransferInfoAsync()
        {
            return await GetAsync<PhuThuongStickyRice.Web.Components.Shop.Models.BankTransferInfoModel>(
                "api/payments/bank-transfer-info");
        }

        /// <summary>Lấy danh sách đăng ký nhắc cúng của khách.</summary>
                public async Task<List<CungReminderModel>> GetCungRemindersAsync()
        {
            return await GetAsync<List<CungReminderModel>>("api/quotes/cung-reminders");
        }

        /// <summary>Bật/tắt một đăng ký nhắc cúng.</summary>
        /// <param name="id">Id đăng ký nhắc cúng.</param>
                public async Task<CungReminderModel> ToggleCungReminderAsync(Guid id)
        {
            return await PutAsync<CungReminderModel>($"api/quotes/cung-reminders/{id}/toggle", new { });
        }

        // Product size options.

        /// <summary>Lấy danh sách tùy chọn cỡ/phân loại của một món.</summary>
        /// <param name="productId">Id món ăn.</param>
        public async Task<List<ProductSizeOptionModel>> GetSizeOptionsAsync(Guid productId)
        {
            return await GetAsync<List<ProductSizeOptionModel>>($"api/product-size-options/by-product/{productId}");
        }

        /// <summary>Tạo tùy chọn cỡ món mới.</summary>
        /// <param name="model">Thông tin tùy chọn cỡ cần tạo.</param>
        public async Task<ProductSizeOptionModel> CreateSizeOptionAsync(ProductSizeOptionModel model)
        {
            return await PostAsync<ProductSizeOptionModel>("api/product-size-options", model);
        }

        /// <summary>Cập nhật tùy chọn cỡ món theo Id.</summary>
        /// <param name="id">Id tùy chọn cỡ.</param>
        /// <param name="model">Thông tin tùy chọn cỡ mới.</param>
        public async Task<ProductSizeOptionModel> UpdateSizeOptionAsync(Guid id, ProductSizeOptionModel model)
        {
            return await PutAsync<ProductSizeOptionModel>($"api/product-size-options/{id}", model);
        }

        /// <summary>Xóa tùy chọn cỡ món theo Id.</summary>
        /// <param name="id">Id tùy chọn cỡ cần xóa.</param>
        public async Task DeleteSizeOptionAsync(Guid id)
        {
            await DeleteAsync($"api/product-size-options/{id}");
        }

        private sealed class PagedResult<T>
        {
            public long TotalCount { get; set; }

            public List<T> Items { get; set; } = new List<T>();
        }
    }
}
