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
    public class CustomerCareService : ApiClientBase
    {
        /// <summary>Khởi tạo client chăm sóc khách hàng với HttpClient và token store.</summary>
        public CustomerCareService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>Admin: khách có sinh nhật trong tháng (để gửi quà/voucher).</summary>
        /// <param name="month">Tháng cần lấy (1-12).</param>
        /// <returns>Danh sách thành viên sinh nhật trong tháng.</returns>
        public async Task<List<BirthdayMemberModel>> GetBirthdaysAsync(int month)
        {
            await SetBearerToken();
            return await GetAsync<List<BirthdayMemberModel>>($"api/admin/customer-care/birthdays?month={month}")
                ?? new List<BirthdayMemberModel>();
        }

        /// <summary>Admin: tặng quà sinh nhật cho 1 khách (sinh voucher giảm giá gắn SĐT).</summary>
        /// <param name="userId">Id khách hàng.</param>
        /// <param name="amount">Số tiền giảm của voucher quà.</param>
        /// <returns>Kết quả tặng quà (mã voucher, số tiền, hạn dùng).</returns>
        public async Task<BirthdayGiftResultModel> GrantBirthdayGiftAsync(Guid userId, decimal amount)
        {
            await SetBearerToken();
            // Dispose response để trả connection về pool ngay sau khi đọc xong.
            using var response = await _httpClient.PostAsJsonAsync(
                $"api/admin/customer-care/birthdays/{userId}/gift",
                new { DiscountAmount = amount });
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await HttpErrorMessageReader.ReadErrorMessageAsync(response, "Thao tác thất bại. Vui lòng thử lại."));
            }

            return await response.Content.ReadFromJsonAsync<BirthdayGiftResultModel>();
        }

        /// <summary>
        /// Item 132: tạo voucher sinh nhật hàng loạt cho khách có sinh nhật trong 7 ngày tới.
        /// </summary>
        public async Task<BirthdayVoucherResultModel> GenerateBirthdayVouchersAsync()
        {
            await SetBearerToken();
            // Dispose response để trả connection về pool ngay sau khi đọc xong.
            using var response = await _httpClient.PostAsync("api/loyalty/birthdays/vouchers?daysAhead=7", null);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Không tạo được voucher. Vui lòng thử lại.");
            }

            return await response.Content.ReadFromJsonAsync<BirthdayVoucherResultModel>();
        }

        /// <summary>Admin: tạo lời nhắc chăm sóc khách (vd nhắc khách lâu không quay lại).</summary>
        /// <param name="phone">SĐT khách hàng.</param>
        /// <param name="customerName">Tên khách hàng.</param>
        /// <param name="message">Nội dung lời nhắc.</param>
        /// <returns>Lời nhắc vừa tạo.</returns>
        public async Task<CustomerReminderModel> CreateReminderAsync(string phone, string customerName, string message)
        {
            await SetBearerToken();
            // Dispose response để trả connection về pool ngay sau khi đọc xong.
            using var response = await _httpClient.PostAsJsonAsync(
                "api/admin/customer-care/reminders",
                new { Phone = phone, CustomerName = customerName, Message = message });
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await HttpErrorMessageReader.ReadErrorMessageAsync(response, "Thao tác thất bại. Vui lòng thử lại."));
            }

            return await response.Content.ReadFromJsonAsync<CustomerReminderModel>();
        }

        /// <summary>
        /// Admin: lời nhắc chăm sóc của 1 SĐT. Lỗi mạng/API được nuốt và trả
        /// danh sách rỗng — trang chăm sóc khách không được vỡ chỉ vì API
        /// lời nhắc hỏng (tính năng phụ).
        /// </summary>
        /// <param name="phone">SĐT khách hàng.</param>
        /// <returns>Danh sách lời nhắc; rỗng nếu lỗi.</returns>
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

    /// <summary>Thành viên có sinh nhật trong tháng kèm trạng thái đã nhận quà năm nay.</summary>
    public class BirthdayMemberModel
    {
        /// <summary>Id khách hàng.</summary>
        public Guid UserId { get; set; }

        /// <summary>SĐT khách hàng.</summary>
        public string Phone { get; set; }

        /// <summary>Tên khách hàng.</summary>
        public string UserName { get; set; }

        /// <summary>Ngày sinh của khách.</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>True nếu khách đã nhận quà sinh nhật năm nay (chống tặng trùng).</summary>
        public bool HasGiftThisYear { get; set; }

        /// <summary>Mã voucher quà đã tặng (nếu có).</summary>
        public string GiftCode { get; set; }
    }

    /// <summary>Kết quả tặng quà sinh nhật cho 1 khách.</summary>
    public class BirthdayGiftResultModel
    {
        /// <summary>Mã voucher giảm giá đã sinh.</summary>
        public string Code { get; set; }

        /// <summary>Số tiền giảm của voucher.</summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>Hạn dùng của voucher.</summary>
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>Kết quả sinh voucher sinh nhật hàng loạt.</summary>
    public class BirthdayVoucherResultModel
    {
        /// <summary>Số voucher đã sinh thành công.</summary>
        public int Created { get; set; }

        /// <summary>Chi tiết từng voucher (SĐT + mã).</summary>
        public List<BirthdayVoucherModel> Vouchers { get; set; }
    }

    /// <summary>Voucher sinh nhật của 1 khách.</summary>
    public class BirthdayVoucherModel
    {
        /// <summary>SĐT chủ voucher.</summary>
        public string Phone { get; set; }

        /// <summary>Mã voucher đã sinh.</summary>
        public string Code { get; set; }
    }

    /// <summary>Lời nhắc chăm sóc khách hàng.</summary>
    public class CustomerReminderModel
    {
        /// <summary>Id lời nhắc.</summary>
        public Guid Id { get; set; }

        /// <summary>SĐT khách được nhắc.</summary>
        public string CustomerPhone { get; set; }

        /// <summary>Tên khách được nhắc.</summary>
        public string CustomerName { get; set; }

        /// <summary>Nội dung lời nhắc.</summary>
        public string Message { get; set; }

        /// <summary>Kênh đã gửi (Zalo/SMS...).</summary>
        public string SentVia { get; set; }

        /// <summary>Người tạo lời nhắc.</summary>
        public string CreatedBy { get; set; }

        /// <summary>Thời điểm tạo.</summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Helper dùng chung để trích thông điệp lỗi thân thiện từ response HTTP:
    /// ưu tiên field "message" trong JSON, nếu không có thì dùng nguyên body
    /// (khi body ngắn), cuối cùng dùng thông điệp dự phòng truyền vào.
    /// </summary>
    internal static class HttpErrorMessageReader
    {
        /// <summary>Đọc thông điệp lỗi thân thiện từ HTTP response.</summary>
        /// <param name="response">Response lỗi từ API.</param>
        /// <param name="fallbackMessage">Thông điệp dự phòng khi không trích được.</param>
        internal static async Task<string> ReadErrorMessageAsync(
            HttpResponseMessage response, string fallbackMessage)
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

            return fallbackMessage;
        }
    }
}
