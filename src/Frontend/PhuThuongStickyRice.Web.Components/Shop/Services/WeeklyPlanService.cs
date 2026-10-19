using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Gói "Ăn sáng cả tuần" — các endpoint public (anonymous).
    /// </summary>
    public class WeeklyPlanService
    {
        // CA1869: dùng chung 1 JsonSerializerOptions thay vì new mỗi lần deserialize
        // (tránh cấp phát + biên dịch metadata lại trong mỗi lần gọi API).
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly HttpClient _httpClient;

        public WeeklyPlanService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>Lấy gói "Ăn sáng cả tuần" đang mở bán (null nếu chưa có).</summary>
        public async Task<WeeklyPlanModel> GetActiveAsync()
        {
            using var response = await _httpClient.GetAsync("api/weekly-plans/active");
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            return await ReadOrThrowAsync<WeeklyPlanModel>(response);
        }

        /// <summary>Mua gói tuần cho khách hàng theo tên và SĐT.</summary>
        public async Task<WeeklyPlanOrderModel> BuyAsync(Guid weeklyPlanId, string customerName, string customerPhone)
        {
            using var response = await _httpClient.PostAsJsonAsync("api/weekly-plans/buy",
                new { WeeklyPlanId = weeklyPlanId, CustomerName = customerName, CustomerPhone = customerPhone });
            return await ReadOrThrowAsync<WeeklyPlanOrderModel>(response);
        }

        /// <summary>Lấy các đơn gói tuần của khách theo SĐT.</summary>
        public async Task<List<WeeklyPlanOrderModel>> GetMyOrdersAsync(string phone)
        {
            using var response = await _httpClient.GetAsync(
                "api/weekly-plans/my-orders?phone=" + Uri.EscapeDataString(phone ?? string.Empty));
            return await ReadOrThrowAsync<List<WeeklyPlanOrderModel>>(response)
                ?? new List<WeeklyPlanOrderModel>();
        }

        /// <summary>Khách "nhận" suất ăn của 1 ngày trong gói tuần.</summary>
        public async Task<WeeklyPlanOrderModel> ClaimAsync(Guid orderId, string day, string phone)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"api/weekly-plans/orders/{orderId}/claim", new { day, phone });
            return await ReadOrThrowAsync<WeeklyPlanOrderModel>(response);
        }

        private static async Task<T> ReadOrThrowAsync<T>(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(ExtractMessage(body)
                    ?? $"Yêu cầu thất bại ({(int)response.StatusCode}).");
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(body, _jsonOptions);
        }

        private static string ExtractMessage(string body)
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body, _jsonOptions);
                if (dict != null && dict.TryGetValue("message", out var el)
                    && el.ValueKind == JsonValueKind.String)
                {
                    return el.GetString();
                }
            }
            catch
            {
                // Bỏ qua, dùng message mặc định.
            }

            return null;
        }
    }
}
