using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Orders.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Admin operations APIs: shippers, kitchen display, blacklist,
    /// customer notes, promotions, reward rules, articles, table QRs,
    /// profit report, login audit and 2FA.
    /// </summary>
    public class AdminOperationsService : HttpService
    {
        /// <summary>Khởi tạo client gọi API vận hành (admin).</summary>
        /// <param name="httpClient">HttpClient đã gắn base address API.</param>
        /// <param name="tokenStore">Quản lý JWT gắn vào header.</param>
        public AdminOperationsService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        // ---- Shippers (item 82) ----

        /// <summary>Lấy danh sách shipper.</summary>
        /// <param name="activeOnly">true: chỉ shipper đang hoạt động.</param>
        public async Task<List<ShipperModel>> GetShippersAsync(bool activeOnly = false)
        {
            return await GetAsync<List<ShipperModel>>($"api/shippers?activeOnly={activeOnly.ToString().ToLowerInvariant()}");
        }

        /// <summary>Tạo shipper mới.</summary>
        /// <param name="name">Tên shipper.</param>
        /// <param name="phone">Số điện thoại shipper.</param>
        public async Task<ShipperModel> CreateShipperAsync(string name, string phone)
        {
            return await PostAsync<ShipperModel>("api/shippers", new { Name = name, Phone = phone });
        }

        /// <summary>Cập nhật thông tin shipper.</summary>
        /// <param name="id">Id shipper.</param>
        /// <param name="name">Tên shipper.</param>
        /// <param name="phone">Số điện thoại shipper.</param>
        /// <param name="isActive">Trạng thái hoạt động.</param>
        public async Task<ShipperModel> UpdateShipperAsync(Guid id, string name, string phone, bool isActive)
        {
            return await PutAsync<ShipperModel>($"api/shippers/{id}",
                new { Name = name, Phone = phone, IsActive = isActive });
        }

        /// <summary>Xóa shipper.</summary>
        /// <param name="id">Id shipper.</param>
        public async Task DeleteShipperAsync(Guid id)
        {
            await DeleteAsync($"api/shippers/{id}");
        }

        /// <summary>Gán shipper cho đơn hàng (null = bỏ gán).</summary>
        /// <param name="orderId">Id đơn hàng.</param>
        /// <param name="shipperId">Id shipper, null để bỏ gán.</param>
        public async Task<OrderModel> AssignShipperAsync(Guid orderId, Guid? shipperId)
        {
            return await PutAsync<OrderModel>($"api/orders/{orderId}/assign-shipper",
                new { ShipperId = shipperId });
        }

        // ---- Kitchen display (item 81) ----

        /// <summary>Lấy đơn cần xử lý cho màn hình bếp (Pending/Confirmed/Preparing, cũ nhất trước).</summary>
        public async Task<List<OrderModel>> GetKitchenOrdersAsync()
        {
            return await GetAsync<List<OrderModel>>("api/orders/kitchen");
        }

        /// <summary>Chuyển đơn sang trạng thái bếp tiếp theo (Pending→Confirmed→Preparing→...).</summary>
        /// <param name="orderId">Id đơn hàng.</param>
        public async Task<OrderModel> AdvanceKitchenStatusAsync(Guid orderId)
        {
            return await PostAsync<OrderModel>($"api/orders/{orderId}/kitchen/advance", new { });
        }

        // ---- Blacklist (item 88) ----

        /// <summary>Lấy danh sách SĐT bị chặn.</summary>
        public async Task<List<BlacklistedPhoneModel>> GetBlacklistAsync()
        {
            return await GetAsync<List<BlacklistedPhoneModel>>("api/blacklist");
        }

        /// <summary>Thêm SĐT vào danh sách chặn.</summary>
        /// <param name="phone">SĐT cần chặn.</param>
        /// <param name="reason">Lý do chặn.</param>
        public async Task<BlacklistedPhoneModel> AddToBlacklistAsync(string phone, string reason)
        {
            return await PostAsync<BlacklistedPhoneModel>("api/blacklist", new { Phone = phone, Reason = reason });
        }

        /// <summary>Gỡ SĐT khỏi danh sách chặn.</summary>
        /// <param name="id">Id bản ghi chặn.</param>
        public async Task RemoveFromBlacklistAsync(Guid id)
        {
            await DeleteAsync($"api/blacklist/{id}");
        }

        // ---- Customer notes (item 89) ----

        /// <summary>Lấy ghi chú nội bộ của khách theo SĐT.</summary>
        /// <param name="phone">SĐT khách hàng.</param>
        public async Task<List<CustomerNoteModel>> GetCustomerNotesAsync(string phone)
        {
            return await GetAsync<List<CustomerNoteModel>>(
                $"api/customernotes?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }

        /// <summary>Thêm ghi chú nội bộ cho khách.</summary>
        /// <param name="phone">SĐT khách hàng.</param>
        /// <param name="note">Nội dung ghi chú.</param>
        public async Task<CustomerNoteModel> AddCustomerNoteAsync(string phone, string note)
        {
            return await PostAsync<CustomerNoteModel>("api/customernotes",
                new { Phone = phone, Note = note });
        }

        /// <summary>Xóa ghi chú nội bộ.</summary>
        /// <param name="id">Id ghi chú.</param>
        public async Task DeleteCustomerNoteAsync(Guid id)
        {
            await DeleteAsync($"api/customernotes/{id}");
        }

        // ---- Promotions (item 91) ----

        /// <summary>Lấy danh sách chương trình khuyến mãi.</summary>
        public async Task<List<PromotionModel>> GetPromotionsAsync()
        {
            return await GetAsync<List<PromotionModel>>("api/promotions");
        }

        /// <summary>Item 91: best currently-active promotion (public endpoint).</summary>
        public async Task<PromotionModel> GetActivePromotionAsync()
        {
            return await GetAsync<PromotionModel>("api/promotions/active");
        }

        /// <summary>Tạo chương trình khuyến mãi mới.</summary>
        /// <param name="model">Thông tin khuyến mãi.</param>
        public async Task<PromotionModel> CreatePromotionAsync(PromotionModel model)
        {
            return await PostAsync<PromotionModel>("api/promotions", model);
        }

        /// <summary>Cập nhật chương trình khuyến mãi.</summary>
        /// <param name="id">Id khuyến mãi.</param>
        /// <param name="model">Thông tin khuyến mãi mới.</param>
        public async Task<PromotionModel> UpdatePromotionAsync(Guid id, PromotionModel model)
        {
            return await PutAsync<PromotionModel>($"api/promotions/{id}", model);
        }

        /// <summary>Xóa chương trình khuyến mãi.</summary>
        /// <param name="id">Id khuyến mãi.</param>
        public async Task DeletePromotionAsync(Guid id)
        {
            await DeleteAsync($"api/promotions/{id}");
        }

        // ---- Reward rules (item 92) ----

        /// <summary>Lấy danh sách quy tắc tích điểm thưởng.</summary>
        public async Task<List<OrderRewardRuleModel>> GetRewardRulesAsync()
        {
            return await GetAsync<List<OrderRewardRuleModel>>("api/rewardrules");
        }

        /// <summary>Tạo quy tắc tích điểm mới.</summary>
        /// <param name="model">Thông tin quy tắc.</param>
        public async Task<OrderRewardRuleModel> CreateRewardRuleAsync(OrderRewardRuleModel model)
        {
            return await PostAsync<OrderRewardRuleModel>("api/rewardrules", model);
        }

        /// <summary>Cập nhật quy tắc tích điểm.</summary>
        /// <param name="id">Id quy tắc.</param>
        /// <param name="model">Thông tin quy tắc mới.</param>
        public async Task<OrderRewardRuleModel> UpdateRewardRuleAsync(Guid id, OrderRewardRuleModel model)
        {
            return await PutAsync<OrderRewardRuleModel>($"api/rewardrules/{id}", model);
        }

        /// <summary>Xóa quy tắc tích điểm.</summary>
        /// <param name="id">Id quy tắc.</param>
        public async Task DeleteRewardRuleAsync(Guid id)
        {
            await DeleteAsync($"api/rewardrules/{id}");
        }

        // ---- Articles (item 93) ----

        /// <summary>Lấy danh sách bài viết/tin tức.</summary>
        public async Task<List<ArticleModel>> GetArticlesAsync()
        {
            return await GetAsync<List<ArticleModel>>("api/articles");
        }

        /// <summary>
        /// Lấy 1 bài viết đầy đủ nội dung (form sửa — list chỉ trả excerpt Body).
        /// </summary>
        /// <param name="id">Id bài viết.</param>
        public async Task<ArticleModel> GetArticleAsync(Guid id)
        {
            return await GetAsync<ArticleModel>($"api/articles/{id}");
        }

        /// <summary>Tạo bài viết mới.</summary>
        /// <param name="model">Thông tin bài viết.</param>
        public async Task<ArticleModel> CreateArticleAsync(ArticleModel model)
        {
            return await PostAsync<ArticleModel>("api/articles", model);
        }

        /// <summary>Cập nhật bài viết.</summary>
        /// <param name="id">Id bài viết.</param>
        /// <param name="model">Thông tin bài viết mới.</param>
        public async Task<ArticleModel> UpdateArticleAsync(Guid id, ArticleModel model)
        {
            return await PutAsync<ArticleModel>($"api/articles/{id}", model);
        }

        /// <summary>Xóa bài viết.</summary>
        /// <param name="id">Id bài viết.</param>
        public async Task DeleteArticleAsync(Guid id)
        {
            await DeleteAsync($"api/articles/{id}");
        }

        // ---- Table QR (item 95) ----

        /// <summary>Lấy danh sách mã QR bàn.</summary>
        public async Task<List<TableQrModel>> GetTableQrsAsync()
        {
            return await GetAsync<List<TableQrModel>>("api/tableqrs");
        }

        /// <summary>Tạo mã QR mới cho bàn/quầy.</summary>
        /// <param name="label">Nhãn hiển thị (VD: "Bàn 1").</param>
        public async Task<TableQrGeneratedModel> GenerateTableQrAsync(string label)
        {
            return await PostAsync<TableQrGeneratedModel>("api/tableqrs", new { Label = label });
        }

        /// <summary>Xóa mã QR bàn.</summary>
        /// <param name="id">Id mã QR.</param>
        public async Task DeleteTableQrAsync(Guid id)
        {
            await DeleteAsync($"api/tableqrs/{id}");
        }

        /// <summary>
        /// Dựng URL ảnh PNG của mã QR từ base address API (client tự tải ảnh,
        /// không qua JSON — dùng cho nút tải/in mã QR).
        /// </summary>
        /// <param name="id">Id mã QR.</param>
        /// <returns>URL tuyệt đối tới file PNG.</returns>
        public string GetTableQrPngUrl(Guid id)
        {
            var baseUri = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
            return $"{baseUri}/api/tableqrs/{id:D}/png";
        }

        // ---- Profit report (item 84) ----

        /// <summary>Lấy báo cáo lãi gộp theo khoảng ngày.</summary>
        /// <param name="from">Từ ngày (null = mặc định backend).</param>
        /// <param name="to">Đến ngày (null = mặc định backend).</param>
        public async Task<ProfitReportModel> GetProfitReportAsync(DateTime? from = null, DateTime? to = null)
        {
            var url = "api/profitreport";
            var sep = "?";
            if (from.HasValue)
            {
                url += $"{sep}from={from.Value:yyyy-MM-dd}";
                sep = "&";
            }

            if (to.HasValue)
            {
                url += $"{sep}to={to.Value:yyyy-MM-dd}";
            }

            return await GetAsync<ProfitReportModel>(url);
        }

        // ---- Login audit (item 99) ----

        /// <summary>Lấy nhật ký đăng nhập (phân trang).</summary>
        /// <param name="page">Trang (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang.</param>
        public async Task<PagedLoginAuditModel> GetLoginAuditsAsync(int page = 1, int pageSize = 50)
        {
            return await GetAsync<PagedLoginAuditModel>($"api/loginaudits?page={page}&pageSize={pageSize}");
        }

        // ---- 2FA (item 98) ----

        /// <summary>Lấy thông tin thiết lập 2FA (secret + QR code) cho tài khoản hiện tại.</summary>
        public async Task<TwoFactorSetupModel> GetTwoFactorSetupAsync()
        {
            return await GetAsync<TwoFactorSetupModel>("api/auth/2fa/setup");
        }

        /// <summary>Bật 2FA sau khi người dùng xác nhận mã OTP từ app authenticator.</summary>
        /// <param name="secret">Secret đã hiển thị ở bước setup.</param>
        /// <param name="code">Mã OTP 6 số từ app authenticator.</param>
        public async Task EnableTwoFactorAsync(string secret, string code)
        {
            await PostAsync<object>("api/auth/2fa/enable", new { Secret = secret, Code = code });
        }

        /// <summary>Tắt 2FA (yêu cầu nhập mã OTP hiện tại để xác thực).</summary>
        /// <param name="code">Mã OTP 6 số hiện tại.</param>
        public async Task DisableTwoFactorAsync(string code)
        {
            await PostAsync<object>("api/auth/2fa/disable", new { Code = code });
        }
    }
}
