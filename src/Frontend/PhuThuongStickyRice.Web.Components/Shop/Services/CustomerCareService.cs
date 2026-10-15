using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Admin customer-care endpoints (api/admin/customer-care): birthday
    /// list + gifts, inactive-customer reminders (Zalo OA stub).
    /// </summary>
    public class CustomerCareService : HttpService
    {
        public CustomerCareService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<List<BirthdayMemberModel>> GetBirthdaysAsync(int month)
        {
            await SetBearerToken();
            return await GetAsync<List<BirthdayMemberModel>>($"api/admin/customer-care/birthdays?month={month}")
                ?? new List<BirthdayMemberModel>();
        }

        public async Task<BirthdayGiftResultModel> GrantBirthdayGiftAsync(Guid userId, decimal amount)
        {
            await SetBearerToken();
            var response = await _httpClient.PostAsJsonAsync(
                $"api/admin/customer-care/birthdays/{userId}/gift",
                new { DiscountAmount = amount });
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await CustomerCareErrorReader.ReadErrorMessageAsync(response));
            }

            return await response.Content.ReadFromJsonAsync<BirthdayGiftResultModel>();
        }

        public async Task<CustomerReminderModel> CreateReminderAsync(string phone, string customerName, string message)
        {
            await SetBearerToken();
            var response = await _httpClient.PostAsJsonAsync(
                "api/admin/customer-care/reminders",
                new { Phone = phone, CustomerName = customerName, Message = message });
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await CustomerCareErrorReader.ReadErrorMessageAsync(response));
            }

            return await response.Content.ReadFromJsonAsync<CustomerReminderModel>();
        }

        public async Task<List<CustomerReminderModel>> GetRemindersAsync(string phone)
        {
            try
            {
                await SetBearerToken();
                return await GetAsync<List<CustomerReminderModel>>(
                        $"api/admin/customer-care/reminders?phone={Uri.EscapeDataString(phone ?? string.Empty)}")
                    ?? new List<CustomerReminderModel>();
            }
            catch (Exception)
            {
                return new List<CustomerReminderModel>();
            }
        }
    }

    public class BirthdayMemberModel
    {
        public Guid UserId { get; set; }

        public string Phone { get; set; }

        public string UserName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public bool HasGiftThisYear { get; set; }

        public string GiftCode { get; set; }
    }

    public class BirthdayGiftResultModel
    {
        public string Code { get; set; }

        public decimal DiscountAmount { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }

    public class CustomerReminderModel
    {
        public Guid Id { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerName { get; set; }

        public string Message { get; set; }

        public string SentVia { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    internal static class CustomerCareErrorReader
    {
        internal static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                var start = body.IndexOf("\"message\"", StringComparison.OrdinalIgnoreCase);
                if (start >= 0)
                {
                    var colon = body.IndexOf(':', start);
                    var quote1 = body.IndexOf('"', colon);
                    var quote2 = body.IndexOf('"', quote1 + 1);
                    if (quote1 >= 0 && quote2 > quote1)
                    {
                        return body.Substring(quote1 + 1, quote2 - quote1 - 1);
                    }
                }

                if (!string.IsNullOrWhiteSpace(body) && body.Length < 300)
                {
                    return body;
                }
            }
            catch (Exception)
            {
                // Fall through to the generic message.
            }

            return "Thao tác thất bại. Vui lòng thử lại.";
        }
    }
}
