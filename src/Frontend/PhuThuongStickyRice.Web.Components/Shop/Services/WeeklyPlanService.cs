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
        private readonly HttpClient _httpClient;

        public WeeklyPlanService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeeklyPlanModel> GetActiveAsync()
        {
            var response = await _httpClient.GetAsync("api/weekly-plans/active");
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            return await ReadOrThrowAsync<WeeklyPlanModel>(response);
        }

        public async Task<WeeklyPlanOrderModel> BuyAsync(Guid weeklyPlanId, string customerName, string customerPhone)
        {
            var response = await _httpClient.PostAsJsonAsync("api/weekly-plans/buy",
                new { WeeklyPlanId = weeklyPlanId, CustomerName = customerName, CustomerPhone = customerPhone });
            return await ReadOrThrowAsync<WeeklyPlanOrderModel>(response);
        }

        public async Task<List<WeeklyPlanOrderModel>> GetMyOrdersAsync(string phone)
        {
            var response = await _httpClient.GetAsync(
                "api/weekly-plans/my-orders?phone=" + Uri.EscapeDataString(phone ?? string.Empty));
            return await ReadOrThrowAsync<List<WeeklyPlanOrderModel>>(response)
                ?? new List<WeeklyPlanOrderModel>();
        }

        public async Task<WeeklyPlanOrderModel> ClaimAsync(Guid orderId, string day)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/weekly-plans/orders/{orderId}/claim", new { day });
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

            return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }

        private static string ExtractMessage(string body)
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
