using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Client gọi các API quản trị (item 121/122/123/124/125/127/128/129/130/131/132/133/139/140):
    /// điểm danh, khách hàng doanh nghiệp, ảnh khách hàng, dự báo nguyên liệu,
    /// ca làm việc, ví, chốt ca, hoàn tiền, mẫu tin nhắn, chiến dịch voucher, POS,
    /// đối soát MoMo và widget dashboard. Mọi method đều gọi HTTP tới WebAPI,
    /// token admin được gắn tự động qua <see cref="ApiClientBase"/>.
    /// </summary>
    public class AdminCommerceService : ApiClientBase
    {
        /// <summary>Khởi tạo client API quản trị với HttpClient và token store.</summary>
        public AdminCommerceService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        // CA1869: JsonSerializerOptions tốn kém khi khởi tạo — cache 1 instance
        // dùng chung thay vì new mỗi lần deserialize.
        private static readonly System.Text.Json.JsonSerializerOptions _caseInsensitiveJsonOptions =
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // ---- Check-in (item 121, public) ----

        /// <summary>Ghi nhận điểm danh cho khách theo SĐT (public — màn điểm danh tại quầy).</summary>
        /// <param name="phone">SĐT khách hàng.</param>
        /// <returns>Kết quả điểm danh (điểm cộng, streak...).</returns>
        public async Task<CheckInResultModel> CheckInAsync(string phone)
        {
            return await PostAsync<CheckInResultModel>("api/checkins", new { phone });
        }

        /// <summary>Kiểm tra trạng thái điểm danh hôm nay của SĐT.</summary>
        /// <param name="phone">SĐT khách hàng.</param>
        /// <returns>Trạng thái điểm danh hiện tại.</returns>
        public async Task<CheckInResultModel> GetCheckInStatusAsync(string phone)
        {
            return await GetAsync<CheckInResultModel>(
                $"api/checkins/status?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }

        // ---- Corporate clients (item 122) ----

        /// <summary>Admin: danh sách khách hàng doanh nghiệp.</summary>
        public async Task<List<CorporateClientModel>> GetCorporateClientsAsync()
        {
            return await GetAsync<List<CorporateClientModel>>("api/corporate-clients");
        }

        /// <summary>Admin: tạo khách hàng doanh nghiệp mới.</summary>
        /// <param name="model">Thông tin khách hàng doanh nghiệp.</param>
        /// <returns>Khách hàng vừa tạo.</returns>
        public async Task<CorporateClientModel> CreateCorporateClientAsync(CorporateClientModel model)
        {
            return await PostAsync<CorporateClientModel>("api/corporate-clients", model);
        }

        /// <summary>Admin: cập nhật khách hàng doanh nghiệp.</summary>
        /// <param name="id">Id khách hàng.</param>
        /// <param name="model">Thông tin mới.</param>
        /// <returns>Khách hàng sau khi cập nhật.</returns>
        public async Task<CorporateClientModel> UpdateCorporateClientAsync(Guid id, CorporateClientModel model)
        {
            return await PutAsync<CorporateClientModel>($"api/corporate-clients/{id}", model);
        }

        /// <summary>Admin: xóa khách hàng doanh nghiệp.</summary>
        /// <param name="id">Id khách hàng cần xóa.</param>
        public async Task DeleteCorporateClientAsync(Guid id)
        {
            await DeleteAsync($"api/corporate-clients/{id}");
        }

        /// <summary>Admin: khách hàng doanh nghiệp còn nợ (dashboard công nợ).</summary>
        public async Task<List<CorporateClientModel>> GetCorporateDueAsync()
        {
            return await GetAsync<List<CorporateClientModel>>("api/dashboard/corporate-due");
        }

        // ---- Customer photos (item 123) ----

        /// <summary>Danh sách ảnh khách hàng đã được duyệt (hiển thị public).</summary>
        public async Task<List<CustomerPhotoModel>> GetApprovedPhotosAsync()
        {
            return await GetAsync<List<CustomerPhotoModel>>("api/customer-photos");
        }

        /// <summary>
        /// Uploads a customer photo (multipart) and registers it for
        /// moderation. Mirrors the LandingService upload pattern.
        /// </summary>
        public async Task<CustomerPhotoModel> UploadCustomerPhotoAsync(
            System.IO.Stream content, string fileName, string contentType, string phone, string caption,
            string name = null)
        {
            await SetBearerToken();

            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(phone ?? string.Empty), "phone");
            form.Add(new StringContent(caption ?? string.Empty), "caption");
            form.Add(new StringContent(name ?? string.Empty), "name");

            // HttpResponseMessage giữ connection/stream bên dưới: dispose ngay
            // sau khi đọc xong body để trả connection về pool.
            using var response = await _httpClient.PostAsync("api/customer-photos/upload", form);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Helper ở ApiClientBase: trích property "message" trong body
                // lỗi JSON; body không phải JSON hoặc không có message thì
                // giữ message mặc định (dùng chung với LandingService).
                var serverMessage = ParseServerErrorMessage(body);
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(serverMessage)
                        ? "Tải ảnh lên thất bại."
                        : serverMessage);
            }

            return System.Text.Json.JsonSerializer.Deserialize<CustomerPhotoModel>(body, _caseInsensitiveJsonOptions);
        }

        /// <summary>Admin: danh sách ảnh khách hàng chờ duyệt (pendingOnly = true chỉ lấy ảnh chưa duyệt).</summary>
        /// <param name="pendingOnly">True: chỉ ảnh chờ duyệt; false: tất cả.</param>
        public async Task<List<CustomerPhotoModel>> GetCustomerPhotosAsync(bool pendingOnly)
        {
            return await GetAsync<List<CustomerPhotoModel>>(
                $"api/customer-photos/admin?pendingOnly={pendingOnly.ToString().ToLowerInvariant()}");
        }

        /// <summary>Admin: duyệt ảnh khách hàng để hiển thị public.</summary>
        /// <param name="id">Id ảnh cần duyệt.</param>
        public async Task ApproveCustomerPhotoAsync(Guid id)
        {
            await PutAsync<object>($"api/customer-photos/{id}/approve", new { });
        }

        /// <summary>Admin: từ chối (xóa) ảnh khách hàng.</summary>
        /// <param name="id">Id ảnh cần từ chối.</param>
        public async Task RejectCustomerPhotoAsync(Guid id)
        {
            await DeleteAsync($"api/customer-photos/{id}");
        }

        // ---- Forecast (item 124) ----

        /// <summary>Admin: dự báo lượng nếp (kg) cần chuẩn bị cho sáng mai.</summary>
        public async Task<IngredientForecastModel> GetIngredientForecastAsync()
        {
            return await GetAsync<IngredientForecastModel>("api/forecast");
        }

        // ---- Work shifts (item 125) ----

        /// <summary>Admin: ca làm việc trong tuần chứa ngày cho trước.</summary>
        /// <param name="date">Ngày bất kỳ trong tuần cần xem; null thì lấy tuần hiện tại.</param>
        public async Task<List<WorkShiftModel>> GetWorkShiftsAsync(DateTime? date)
        {
            var dateString = (date ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return await GetAsync<List<WorkShiftModel>>($"api/work-shifts/week?date={dateString}");
        }

        /// <summary>Admin: tạo ca làm việc mới.</summary>
        /// <param name="model">Thông tin ca làm việc.</param>
        /// <returns>Ca làm việc vừa tạo.</returns>
        public async Task<WorkShiftModel> CreateWorkShiftAsync(WorkShiftModel model)
        {
            return await PostAsync<WorkShiftModel>("api/work-shifts", model);
        }

        /// <summary>Admin: cập nhật ca làm việc.</summary>
        /// <param name="id">Id ca làm việc.</param>
        /// <param name="model">Thông tin mới.</param>
        /// <returns>Ca làm việc sau khi cập nhật.</returns>
        public async Task<WorkShiftModel> UpdateWorkShiftAsync(Guid id, WorkShiftModel model)
        {
            return await PutAsync<WorkShiftModel>($"api/work-shifts/{id}", model);
        }

        /// <summary>Admin: xóa ca làm việc.</summary>
        /// <param name="id">Id ca làm việc cần xóa.</param>
        public async Task DeleteWorkShiftAsync(Guid id)
        {
            await DeleteAsync($"api/work-shifts/{id}");
        }

        // ---- Wallet (item 140) ----

        /// <summary>Admin: số dư ví của khách theo SĐT.</summary>
        /// <param name="phone">SĐT khách hàng.</param>
        public async Task<WalletAccountModel> GetWalletBalanceAsync(string phone)
        {
            return await GetAsync<WalletAccountModel>(
                $"api/wallets/balance?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }

        /// <summary>Admin: danh sách ví, tìm kiếm theo SĐT (rỗng thì lấy tất cả).</summary>
        /// <param name="search">Chuỗi tìm kiếm theo SĐT.</param>
        public async Task<List<WalletAccountModel>> GetWalletsAsync(string search)
        {
            var queryString = string.IsNullOrWhiteSpace(search)
                ? string.Empty
                : $"?search={Uri.EscapeDataString(search)}";
            return await GetAsync<List<WalletAccountModel>>($"api/wallets{queryString}");
        }

        /// <summary>Admin: lịch sử giao dịch ví của khách theo SĐT.</summary>
        /// <param name="phone">SĐT khách hàng.</param>
        public async Task<List<WalletTransactionModel>> GetWalletTransactionsAsync(string phone)
        {
            return await GetAsync<List<WalletTransactionModel>>(
                $"api/wallets/transactions?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }

        /// <summary>Admin: cộng/trừ số dư ví thủ công (thưởng bù, điều chỉnh sai sót).</summary>
        /// <param name="phone">SĐT chủ ví.</param>
        /// <param name="amount">Số tiền điều chỉnh (âm = trừ).</param>
        /// <param name="reason">Lý do điều chỉnh (ghi vào lịch sử).</param>
        /// <returns>Ví sau khi điều chỉnh.</returns>
        public async Task<WalletAccountModel> AdjustWalletAsync(string phone, decimal amount, string reason)
        {
            return await PostAsync<WalletAccountModel>("api/wallets/adjust",
                new { phone, amount, reason });
        }

        // ---- Shift close (item 129) ----

        /// <summary>Admin: tiền COD kỳ vọng phải thu trong ngày (tổng đơn COD chưa chốt ca).</summary>
        /// <param name="date">Ngày cần đối chiếu.</param>
        public async Task<decimal> GetExpectedCodAsync(DateTime date)
        {
            var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var result = await GetAsync<Dictionary<string, decimal>>($"api/shift-closes/expected?date={dateString}");
            return result != null && result.TryGetValue("expected", out var expectedCod) ? expectedCod : 0;
        }

        /// <summary>Admin: bản chốt ca của ngày (null nếu ca chưa chốt).</summary>
        /// <param name="date">Ngày cần xem.</param>
        public async Task<ShiftCloseModel> GetShiftCloseAsync(DateTime date)
        {
            var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return await GetAsync<ShiftCloseModel>($"api/shift-closes/by-date?date={dateString}");
        }

        /// <summary>Admin: lịch sử các lần chốt ca.</summary>
        public async Task<List<ShiftCloseModel>> GetShiftClosesAsync()
        {
            return await GetAsync<List<ShiftCloseModel>>("api/shift-closes");
        }

        /// <summary>Admin: chốt ca — đối chiếu tiền COD thực thu với kỳ vọng.</summary>
        /// <param name="date">Ngày chốt ca.</param>
        /// <param name="actualCod">Tiền COD thực tế thu được.</param>
        /// <param name="note">Ghi chú chênh lệch (nếu có).</param>
        /// <returns>Bản chốt ca vừa tạo.</returns>
        public async Task<ShiftCloseModel> CloseShiftAsync(DateTime date, decimal actualCod, string note)
        {
            return await PostAsync<ShiftCloseModel>("api/shift-closes/close",
                new { date, actualCod, note });
        }

        // ---- Refunds (item 130) ----

        /// <summary>Admin: báo cáo các yêu cầu hoàn tiền.</summary>
        public async Task<List<RefundReportRowModel>> GetRefundReportAsync()
        {
            return await GetAsync<List<RefundReportRowModel>>("api/refunds");
        }

        /// <summary>Admin: tạo yêu cầu hoàn tiền cho đơn hàng.</summary>
        /// <param name="orderId">Id đơn hàng cần hoàn.</param>
        /// <param name="amount">Số tiền hoàn.</param>
        /// <param name="reason">Lý do hoàn tiền.</param>
        public async Task RequestRefundAsync(Guid orderId, decimal amount, string reason)
        {
            await PostAsync<object>($"api/refunds/orders/{orderId}/request", new { amount, reason });
        }

        /// <summary>Admin: xác nhận đã hoàn tiền cho đơn hàng.</summary>
        /// <param name="orderId">Id đơn hàng đã hoàn tiền.</param>
        public async Task ConfirmRefundAsync(Guid orderId)
        {
            await PostAsync<object>($"api/refunds/orders/{orderId}/confirm", new { });
        }

        // ---- Message templates (item 131) ----

        /// <summary>Admin: danh sách mẫu tin nhắn, lọc theo kênh (Zalo/SMS...); rỗng thì lấy tất cả.</summary>
        /// <param name="channel">Kênh gửi; null thì lấy mọi kênh.</param>
        public async Task<List<MessageTemplateModel>> GetMessageTemplatesAsync(string channel = null)
        {
            var queryString = string.IsNullOrWhiteSpace(channel)
                ? string.Empty
                : $"?channel={Uri.EscapeDataString(channel)}";
            return await GetAsync<List<MessageTemplateModel>>($"api/message-templates{queryString}");
        }

        /// <summary>Admin: tạo mẫu tin nhắn mới.</summary>
        /// <param name="model">Nội dung mẫu tin nhắn.</param>
        /// <returns>Mẫu tin nhắn vừa tạo.</returns>
        public async Task<MessageTemplateModel> CreateMessageTemplateAsync(MessageTemplateModel model)
        {
            return await PostAsync<MessageTemplateModel>("api/message-templates", model);
        }

        /// <summary>Admin: cập nhật mẫu tin nhắn.</summary>
        /// <param name="id">Id mẫu tin nhắn.</param>
        /// <param name="model">Nội dung mới.</param>
        /// <returns>Mẫu tin nhắn sau khi cập nhật.</returns>
        public async Task<MessageTemplateModel> UpdateMessageTemplateAsync(Guid id, MessageTemplateModel model)
        {
            return await PutAsync<MessageTemplateModel>($"api/message-templates/{id}", model);
        }

        /// <summary>Admin: xóa mẫu tin nhắn.</summary>
        /// <param name="id">Id mẫu tin nhắn cần xóa.</param>
        public async Task DeleteMessageTemplateAsync(Guid id)
        {
            await DeleteAsync($"api/message-templates/{id}");
        }

        // ---- Voucher campaigns (item 132) ----

        /// <summary>Admin: danh sách đối tượng nhận voucher (lọc theo hạng thành viên).</summary>
        /// <param name="tier">Hạng thành viên; rỗng thì lấy tất cả.</param>
        public async Task<List<VoucherCampaignAudienceModel>> GetVoucherAudiencesAsync(string tier)
        {
            var queryString = string.IsNullOrWhiteSpace(tier)
                ? string.Empty
                : $"?tier={Uri.EscapeDataString(tier)}";
            return await GetAsync<List<VoucherCampaignAudienceModel>>($"api/voucher-campaigns/audiences{queryString}");
        }

        /// <summary>Admin: sinh voucher hàng loạt cho chiến dịch (mỗi khách 1 mã dùng 1 lần).</summary>
        /// <param name="audience">Nhóm đối tượng nhận.</param>
        /// <param name="tier">Hạng thành viên áp dụng.</param>
        /// <param name="discountPercent">% giảm (nếu giảm theo %).</param>
        /// <param name="discountAmount">Số tiền giảm (nếu giảm theo tiền).</param>
        /// <param name="validDays">Số ngày hiệu lực của voucher.</param>
        /// <param name="prefix">Tiền tố mã voucher.</param>
        /// <returns>Danh sách voucher đã sinh.</returns>
        public async Task<List<GeneratedVoucherModel>> GenerateVouchersAsync(
            string audience, string tier, decimal discountPercent, decimal discountAmount,
            int validDays, string prefix)
        {
            return await PostAsync<List<GeneratedVoucherModel>>("api/voucher-campaigns/generate",
                new { audience, tier, discountPercent, discountAmount, validDays, prefix });
        }

        // ---- POS (item 127) ----

        /// <summary>Tạo đơn POS tại quầy (thanh toán tiền mặt, nhận hàng ngay).</summary>
        /// <typeparam name="T">Kiểu DTO đơn hàng trả về.</typeparam>
        /// <param name="input">Dữ liệu đơn POS.</param>
        /// <returns>Đơn hàng đã tạo (kiểu <typeparamref name="T"/>).</returns>
        public async Task<T> CreatePosOrderAsync<T>(object input)
        {
            return await PostAsync<T>("api/pos/orders", input);
        }

        // ---- MoMo reconciliation (item 128) ----

        /// <summary>Admin: đơn thanh toán MoMo để đối soát; matched = null lấy tất cả.</summary>
        /// <param name="matched">True: chỉ đơn đã khớp; false: chỉ đơn chưa khớp; null: tất cả.</param>
        public async Task<List<MoMoOrderModel>> GetMoMoOrdersAsync(bool? matched)
        {
            var queryString = matched.HasValue
                ? $"?matched={matched.Value.ToString().ToLowerInvariant()}"
                : string.Empty;
            return await GetAsync<List<MoMoOrderModel>>($"api/momo-reconciliation/orders{queryString}");
        }

        /// <summary>Admin: đánh dấu đơn MoMo đã đối soát khớp thủ công.</summary>
        /// <param name="orderId">Id đơn hàng.</param>
        public async Task MarkMoMoMatchedAsync(Guid orderId)
        {
            await PostAsync<object>($"api/momo-reconciliation/orders/{orderId}/mark-matched", new { });
        }

        // ---- Dashboard widgets (items 122/124/133/139) ----

        /// <summary>
        /// Builds an absolute URL for an /uploads/... path using the API
        /// base address (same pattern as LandingService.ResolveImageUrl).
        /// </summary>
        public string ResolveImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/');
            var path = url.StartsWith('/') ? url : "/" + url;
            return string.IsNullOrEmpty(baseUrl) ? path : baseUrl + path;
        }

        /// <summary>Admin: mục tiêu doanh thu ngày hôm nay (widget dashboard).</summary>
        public async Task<DailyTargetModel> GetDailyTargetAsync()
        {
            return await GetAsync<DailyTargetModel>("api/dashboard/daily-target");
        }

        /// <summary>Admin: thống kê đơn theo nguồn (web/POS/...) trong khoảng thời gian.</summary>
        /// <param name="from">Từ ngày (null = không giới hạn).</param>
        /// <param name="to">Đến ngày (null = không giới hạn).</param>
        public async Task<List<SourceStatModel>> GetSourceStatsAsync(DateTime? from, DateTime? to)
        {
            var queryString = string.Empty;
            if (from.HasValue)
            {
                queryString += $"from={from.Value:yyyy-MM-dd}";
            }

            if (to.HasValue)
            {
                queryString += (queryString.Length > 0 ? "&" : string.Empty) + $"to={to.Value:yyyy-MM-dd}";
            }

            return await GetAsync<List<SourceStatModel>>(
                $"api/dashboard/source-stats{(queryString.Length > 0 ? "?" + queryString : string.Empty)}");
        }
    }
}
