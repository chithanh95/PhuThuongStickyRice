using PhuThuongStickyRice.Blazor.Modules.Admin.Models;
using PhuThuongStickyRice.Blazor.Modules.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Admin.Services
{
    /// <summary>
    /// Wave 5 APIs: check-in, corporate clients, customer photos, forecast,
    /// work shifts, wallet, shift close, refunds, message templates, voucher
    /// campaigns, POS, MoMo reconciliation, dashboard widgets.
    /// </summary>
    public class Wave5Service : HttpService
    {
        public Wave5Service(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        // ---- Check-in (item 121, public) ----

        public async Task<CheckInResultModel> CheckInAsync(string phone)
        {
            return await PostAsync<CheckInResultModel>("api/checkins", new { phone });
        }

        public async Task<CheckInResultModel> GetCheckInStatusAsync(string phone)
        {
            return await GetAsync<CheckInResultModel>(
                $"api/checkins/status?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }

        // ---- Corporate clients (item 122) ----

        public async Task<List<CorporateClientModel>> GetCorporateClientsAsync()
        {
            return await GetAsync<List<CorporateClientModel>>("api/corporate-clients");
        }

        public async Task<CorporateClientModel> CreateCorporateClientAsync(CorporateClientModel model)
        {
            return await PostAsync<CorporateClientModel>("api/corporate-clients", model);
        }

        public async Task<CorporateClientModel> UpdateCorporateClientAsync(Guid id, CorporateClientModel model)
        {
            return await PutAsync<CorporateClientModel>($"api/corporate-clients/{id}", model);
        }

        public async Task DeleteCorporateClientAsync(Guid id)
        {
            await DeleteAsync($"api/corporate-clients/{id}");
        }

        public async Task<List<CorporateClientModel>> GetCorporateDueAsync()
        {
            return await GetAsync<List<CorporateClientModel>>("api/dashboard/corporate-due");
        }

        // ---- Customer photos (item 123) ----

        public async Task<List<CustomerPhotoModel>> GetApprovedPhotosAsync()
        {
            return await GetAsync<List<CustomerPhotoModel>>("api/customer-photos");
        }

        /// <summary>
        /// Uploads a customer photo (multipart) and registers it for
        /// moderation. Mirrors the LandingService upload pattern.
        /// </summary>
        public async Task<CustomerPhotoModel> UploadCustomerPhotoAsync(
            System.IO.Stream content, string fileName, string contentType, string phone, string caption)
        {
            await SetBearerToken();

            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(phone ?? string.Empty), "phone");
            form.Add(new StringContent(caption ?? string.Empty), "caption");

            var response = await _httpClient.PostAsync("api/customer-photos/upload", form);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var message = "Tải ảnh lên thất bại.";
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var msg))
                    {
                        message = msg.GetString() ?? message;
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                }

                throw new InvalidOperationException(message);
            }

            return System.Text.Json.JsonSerializer.Deserialize<CustomerPhotoModel>(body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<List<CustomerPhotoModel>> GetCustomerPhotosAsync(bool pendingOnly)
        {
            return await GetAsync<List<CustomerPhotoModel>>(
                $"api/customer-photos/admin?pendingOnly={pendingOnly.ToString().ToLowerInvariant()}");
        }

        public async Task ApproveCustomerPhotoAsync(Guid id)
        {
            await PutAsync<object>($"api/customer-photos/{id}/approve", new { });
        }

        public async Task RejectCustomerPhotoAsync(Guid id)
        {
            await DeleteAsync($"api/customer-photos/{id}");
        }

        // ---- Forecast (item 124) ----

        public async Task<IngredientForecastModel> GetForecastAsync()
        {
            return await GetAsync<IngredientForecastModel>("api/forecast");
        }

        // ---- Work shifts (item 125) ----

        public async Task<List<WorkShiftModel>> GetWorkShiftsAsync(DateTime? date)
        {
            var d = (date ?? DateTime.Today).ToString("yyyy-MM-dd");
            return await GetAsync<List<WorkShiftModel>>($"api/work-shifts/week?date={d}");
        }

        public async Task<WorkShiftModel> CreateWorkShiftAsync(WorkShiftModel model)
        {
            return await PostAsync<WorkShiftModel>("api/work-shifts", model);
        }

        public async Task<WorkShiftModel> UpdateWorkShiftAsync(Guid id, WorkShiftModel model)
        {
            return await PutAsync<WorkShiftModel>($"api/work-shifts/{id}", model);
        }

        public async Task DeleteWorkShiftAsync(Guid id)
        {
            await DeleteAsync($"api/work-shifts/{id}");
        }

        // ---- Wallet (item 140) ----

        public async Task<WalletAccountModel> GetWalletBalanceAsync(string phone)
        {
            return await GetAsync<WalletAccountModel>(
                $"api/wallets/balance?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }

        public async Task<List<WalletAccountModel>> GetWalletsAsync(string search)
        {
            var q = string.IsNullOrWhiteSpace(search)
                ? string.Empty
                : $"?search={Uri.EscapeDataString(search)}";
            return await GetAsync<List<WalletAccountModel>>($"api/wallets{q}");
        }

        public async Task<List<WalletTransactionModel>> GetWalletTransactionsAsync(string phone)
        {
            return await GetAsync<List<WalletTransactionModel>>(
                $"api/wallets/transactions?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }

        public async Task<WalletAccountModel> AdjustWalletAsync(string phone, decimal amount, string reason)
        {
            return await PostAsync<WalletAccountModel>("api/wallets/adjust",
                new { phone, amount, reason });
        }

        // ---- Shift close (item 129) ----

        public async Task<decimal> GetExpectedCodAsync(DateTime date)
        {
            var d = date.ToString("yyyy-MM-dd");
            var result = await GetAsync<Dictionary<string, decimal>>($"api/shift-closes/expected?date={d}");
            return result != null && result.TryGetValue("expected", out var v) ? v : 0;
        }

        public async Task<ShiftCloseModel> GetShiftCloseAsync(DateTime date)
        {
            var d = date.ToString("yyyy-MM-dd");
            return await GetAsync<ShiftCloseModel>($"api/shift-closes/by-date?date={d}");
        }

        public async Task<List<ShiftCloseModel>> GetShiftClosesAsync()
        {
            return await GetAsync<List<ShiftCloseModel>>("api/shift-closes");
        }

        public async Task<ShiftCloseModel> CloseShiftAsync(DateTime date, decimal actualCod, string note)
        {
            return await PostAsync<ShiftCloseModel>("api/shift-closes/close",
                new { date, actualCod, note });
        }

        // ---- Refunds (item 130) ----

        public async Task<List<RefundReportRowModel>> GetRefundReportAsync()
        {
            return await GetAsync<List<RefundReportRowModel>>("api/refunds");
        }

        public async Task RequestRefundAsync(Guid orderId, decimal amount, string reason)
        {
            await PostAsync<object>($"api/refunds/orders/{orderId}/request", new { amount, reason });
        }

        public async Task ConfirmRefundAsync(Guid orderId)
        {
            await PostAsync<object>($"api/refunds/orders/{orderId}/confirm", new { });
        }

        // ---- Message templates (item 131) ----

        public async Task<List<MessageTemplateModel>> GetMessageTemplatesAsync(string channel = null)
        {
            var q = string.IsNullOrWhiteSpace(channel)
                ? string.Empty
                : $"?channel={Uri.EscapeDataString(channel)}";
            return await GetAsync<List<MessageTemplateModel>>($"api/message-templates{q}");
        }

        public async Task<MessageTemplateModel> CreateMessageTemplateAsync(MessageTemplateModel model)
        {
            return await PostAsync<MessageTemplateModel>("api/message-templates", model);
        }

        public async Task<MessageTemplateModel> UpdateMessageTemplateAsync(Guid id, MessageTemplateModel model)
        {
            return await PutAsync<MessageTemplateModel>($"api/message-templates/{id}", model);
        }

        public async Task DeleteMessageTemplateAsync(Guid id)
        {
            await DeleteAsync($"api/message-templates/{id}");
        }

        // ---- Voucher campaigns (item 132) ----

        public async Task<List<VoucherCampaignAudienceModel>> GetVoucherAudiencesAsync(string tier)
        {
            var q = string.IsNullOrWhiteSpace(tier)
                ? string.Empty
                : $"?tier={Uri.EscapeDataString(tier)}";
            return await GetAsync<List<VoucherCampaignAudienceModel>>($"api/voucher-campaigns/audiences{q}");
        }

        public async Task<List<GeneratedVoucherModel>> GenerateVouchersAsync(
            string audience, string tier, decimal discountPercent, decimal discountAmount,
            int validDays, string prefix)
        {
            return await PostAsync<List<GeneratedVoucherModel>>("api/voucher-campaigns/generate",
                new { audience, tier, discountPercent, discountAmount, validDays, prefix });
        }

        // ---- POS (item 127) ----

        public async Task<T> CreatePosOrderAsync<T>(object input)
        {
            return await PostAsync<T>("api/pos/orders", input);
        }

        // ---- MoMo reconciliation (item 128) ----

        public async Task<List<MoMoOrderModel>> GetMoMoOrdersAsync(bool? matched)
        {
            var q = matched.HasValue
                ? $"?matched={matched.Value.ToString().ToLowerInvariant()}"
                : string.Empty;
            return await GetAsync<List<MoMoOrderModel>>($"api/momo-reconciliation/orders{q}");
        }

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
            var path = url.StartsWith("/") ? url : "/" + url;
            return string.IsNullOrEmpty(baseUrl) ? path : baseUrl + path;
        }
        public async Task<DailyTargetModel> GetDailyTargetAsync()
        {
            return await GetAsync<DailyTargetModel>("api/dashboard/daily-target");
        }

        public async Task<List<SourceStatModel>> GetSourceStatsAsync(DateTime? from, DateTime? to)
        {
            var q = string.Empty;
            if (from.HasValue)
            {
                q += $"from={from.Value:yyyy-MM-dd}";
            }

            if (to.HasValue)
            {
                q += (q.Length > 0 ? "&" : string.Empty) + $"to={to.Value:yyyy-MM-dd}";
            }

            return await GetAsync<List<SourceStatModel>>(
                $"api/dashboard/source-stats{(q.Length > 0 ? "?" + q : string.Empty)}");
        }
    }
}
