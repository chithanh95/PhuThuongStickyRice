using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Shop customer-engagement APIs: loyalty, lucky spin, order
    /// self-service, reviews, support tickets, FAQ, subscriptions
    /// and third-party integrations.
    /// </summary>
    public class ShopEngagementService : ApiClientBase
    {
        public ShopEngagementService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        // ---- Phone OTP ----

        /// <summary>
        /// Xin mã OTP 6 số gửi về SĐT cho 1 thao tác nhạy cảm (purpose:
        /// "address-delete" / "order-history" / "checkout-spend"). Mã có
        /// hiệu lực 5 phút, dùng 1 lần. Ném
        /// <see cref="InvalidOperationException"/> kèm thông báo tiếng Việt
        /// của server khi xin mã thất bại (SĐT sai định dạng, quá 3
        /// lần/giờ...).
        /// </summary>
        /// <param name="phone">SĐT nhận mã OTP.</param>
        /// <param name="purpose">Mục đích xác thực (khớp với thao tác sẽ thực hiện).</param>
        public async Task SendPhoneOtpAsync(string phone, string purpose)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/phone-otp/send", new { Phone = phone, Purpose = purpose });
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    ParseServerErrorMessage(body) ?? "Không gửi được mã OTP. Vui lòng thử lại.");
            }
        }

        // ---- Loyalty ----

        /// <summary>
        /// Lấy tài khoản tích điểm của khách theo SĐT; trả về null khi khách chưa có tài khoản (API trả 204).
        /// Bảo mật: caller ẩn danh chỉ nhận bản TỐI THIỂU (hạng + tên) —
        /// model trả về có IsSummaryOnly = true và các field điểm/tổng
        /// chi tiêu là mặc định. Bản đầy đủ chỉ về khi đang đăng nhập bằng
        /// đúng SĐT đó (JWT được đính kèm tự động khi có phiên đăng nhập).
        /// </summary>
        /// <param name="phone">Số điện thoại khách hàng.</param>
        /// <returns>Tài khoản tích điểm (đầy đủ hoặc bản tối thiểu) hoặc null.</returns>
        public async Task<LoyaltyAccountModel> GetLoyaltyByPhoneAsync(string phone)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"api/loyalty/by-phone?phone={Uri.EscapeDataString(phone ?? string.Empty)}");

            // Đính kèm JWT khi có phiên đăng nhập: server dựa vào đây để
            // quyết định trả bản đầy đủ cho chính chủ SĐT.
            var accessToken = await GetAccessToken();
            if (accessToken != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            using var response = await _httpClient.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var account = JsonSerializer.Deserialize<LoyaltyAccountModel>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (account != null)
            {
                // Bản đầy đủ có field "points"; bản tối thiểu công khai thì
                // không — đánh dấu để UI không hiển thị điểm/tổng chi tiêu
                // mặc định như số thật của khách.
                account.IsSummaryOnly = !HasJsonProperty(json, "points");
            }

            return account;
        }

        /// <summary>
        /// Kiểm tra mã giới thiệu ngay tại checkout để báo lỗi trên màn hình
        /// thay vì chờ tới lúc tạo đơn (server vẫn validate lại khi tạo đơn).
        /// Trả null khi gọi API lỗi — caller coi như chưa kiểm tra được và
        /// không chặn đặt đơn chỉ vì lỗi mạng.
        /// </summary>
        /// <param name="code">Mã giới thiệu khách nhập.</param>
        /// <param name="phone">SĐT người mua (để chặn mã của chính mình).</param>
        /// <returns>Kết quả kiểm tra, hoặc null khi không gọi được API.</returns>
        public async Task<ReferralCheckModel> CheckReferralCodeAsync(string code, string phone)
        {
            try
            {
                return await GetAsync<ReferralCheckModel>(
                    $"api/loyalty/referral-check?code={Uri.EscapeDataString(code ?? string.Empty)}&phone={Uri.EscapeDataString(phone ?? string.Empty)}");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra JSON có property tên <paramref name="name"/> không
        /// (không phân biệt hoa/thường) — dùng để phân biệt response loyalty
        /// đầy đủ với bản tối thiểu công khai.
        /// </summary>
        /// <param name="json">Chuỗi JSON cần kiểm tra.</param>
        /// <param name="name">Tên property cần tìm.</param>
        /// <returns>true khi JSON có property đó ở cấp ngoài cùng.</returns>
        private static bool HasJsonProperty(string json, string name)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                // JSON hỏng: coi như bản tối thiểu cho an toàn hiển thị.
            }

            return false;
        }

        /// <summary>
        /// Tìm kiếm danh sách tài khoản tích điểm theo từ khóa (dùng ở màn admin).
        /// </summary>
        /// <param name="searchTerm">Từ khóa tìm theo SĐT/tên; bỏ trống để lấy tất cả.</param>
        /// <returns>Danh sách tài khoản tích điểm.</returns>
        public async Task<List<LoyaltyAccountModel>> GetLoyaltyListAsync(string searchTerm)
        {
            var url = "api/loyalty";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"?searchTerm={Uri.EscapeDataString(searchTerm)}";
            }

            return await GetAsync<List<LoyaltyAccountModel>>(url);
        }

        /// <summary>
        /// Cộng/trừ điểm tích lũy thủ công cho khách (delta âm là trừ điểm), kèm lý do để đối soát.
        /// </summary>
        /// <param name="phone">Số điện thoại khách hàng.</param>
        /// <param name="delta">Số điểm thay đổi, âm là trừ.</param>
        /// <param name="reason">Lý do điều chỉnh điểm.</param>
        /// <returns>Tài khoản tích điểm sau khi điều chỉnh.</returns>
        public async Task<LoyaltyAccountModel> AdjustPointsAsync(string phone, int delta, string reason)
        {
            return await PutAsync<LoyaltyAccountModel>(
                $"api/loyalty/{Uri.EscapeDataString(phone)}/adjust",
                new AdjustPointsModel { PointsDelta = delta, Reason = reason });
        }

        /// <summary>
        /// Lấy danh sách khách sắp đến sinh nhật trong N ngày tới để gửi ưu đãi sinh nhật.
        /// </summary>
        /// <param name="daysAhead">Số ngày tính từ hôm nay.</param>
        /// <returns>Danh sách tài khoản sắp sinh nhật.</returns>
        public async Task<List<LoyaltyAccountModel>> GetUpcomingBirthdaysAsync(int daysAhead)
        {
            return await GetAsync<List<LoyaltyAccountModel>>($"api/loyalty/birthdays?daysAhead={daysAhead}");
        }

        // ---- Lucky spin ----

        /// <summary>
        /// Gọi quay vòng quay may mắn sau checkout — server mới là nơi quay thật và sinh voucher, frontend chỉ chạy hiệu ứng theo kết quả trả về.
        /// </summary>
        /// <param name="orderId">Id đơn hàng vừa đặt (mỗi đơn chỉ được quay một lần).</param>
        /// <param name="phone">SĐT khách để xác thực và gắn voucher.</param>
        /// <returns>Kết quả quay: segment trúng, nhãn giải và mã voucher.</returns>
        public async Task<SpinResultModel> SpinAsync(Guid orderId, string phone)
        {
            return await PostAsync<SpinResultModel>("api/orders/spin",
                new { OrderId = orderId, CustomerPhone = phone });
        }

        // ---- Order self-service ----

        /// <summary>
        /// Khách tự hủy đơn bằng mã đơn + SĐT. Item 233: bắt buộc chọn lý do hủy.
        /// </summary>
        /// <param name="orderNumber">Mã đơn hàng.</param>
        /// <param name="phone">SĐT khách để xác thực.</param>
        /// <param name="cancelReason">Lý do hủy đơn (bắt buộc).</param>
        /// <returns>Đơn hàng sau khi hủy.</returns>
        public async Task<TrackedOrderModel> CancelOrderByCustomerAsync(
            string orderNumber, string phone, string cancelReason)
        {
            return await PostAsync<TrackedOrderModel>("api/orders/track/cancel",
                new { OrderNumber = orderNumber, CustomerPhone = phone, CancelReason = cancelReason });
        }

        /// <summary>
        /// Khách tự sửa số lượng món của đơn bằng mã đơn + SĐT.
        /// </summary>
        /// <param name="orderNumber">Mã đơn hàng.</param>
        /// <param name="phone">SĐT khách để xác thực.</param>
        /// <param name="lines">Danh sách món với số lượng mới.</param>
        /// <returns>Đơn hàng sau khi cập nhật.</returns>
        public async Task<TrackedOrderModel> UpdateOrderLinesAsync(
            string orderNumber, string phone, List<object> lines)
        {
            return await PutAsync<TrackedOrderModel>("api/orders/track/lines",
                new { OrderNumber = orderNumber, CustomerPhone = phone, Lines = lines });
        }

        /// <summary>
        /// TC3: customer self-service — change address / delivery slot of a
        /// still-Pending order (order number + phone must match).
        /// </summary>
        public async Task<TrackedOrderModel> UpdateAddressByCustomerAsync(
            string orderNumber, string phone, string address, string deliverySlot)
        {
            return await PutAsync<TrackedOrderModel>("api/orders/track/address",
                new { OrderNumber = orderNumber, CustomerPhone = phone, CustomerAddress = address, DeliverySlot = deliverySlot });
        }

        /// <summary>
        /// Item 138: customer self-service — update recipient phone + notes of a Pending order.
        /// </summary>
        public async Task<TrackedOrderModel> UpdateContactByCustomerAsync(
            string orderNumber, string phone, string recipientPhone, string notes)
        {
            return await PutAsync<TrackedOrderModel>("api/orders/track/contact",
                new { OrderNumber = orderNumber, CustomerPhone = phone, RecipientPhone = recipientPhone, Notes = notes });
        }

        /// <summary>
        /// Submits the post-delivery survey. dishRatings: optional per-dish
        /// stars as {ProductId, Rating} objects (Wave 6b, item 26).
        /// </summary>
        public async Task<OrderReviewModel> SubmitReviewAsync(
            string orderNumber, string phone, int rating, string comment,
            List<object> dishRatings = null, string photoUrl = null)
        {
            return await PostAsync<OrderReviewModel>("api/orders/track/review",
                new
                {
                    OrderNumber = orderNumber,
                    CustomerPhone = phone,
                    Rating = rating,
                    Comment = comment,
                    Lines = dishRatings ?? new List<object>(),
                    PhotoUrl = photoUrl
                });
        }

        /// <summary>
        /// Item 44: uploads a review photo (compressed client-side before upload).
        /// Returns the public photo URL, or null when upload failed.
        /// </summary>
        public async Task<string> UploadReviewPhotoAsync(Stream imageStream, string fileName, string contentType)
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(imageStream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(_pendingReviewOrderNumber ?? string.Empty), "orderNumber");
            form.Add(new StringContent(_pendingReviewPhone ?? string.Empty), "customerPhone");

            using var response = await _httpClient.PostAsync("api/orders/track/review/photo", form);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ReviewPhotoUploadResult>();
            return result?.Url;
        }

        /// <summary>Item 44: context for the review-photo upload endpoint.</summary>
        public void SetReviewPhotoContext(string orderNumber, string phone)
        {
            _pendingReviewOrderNumber = orderNumber;
            _pendingReviewPhone = phone;
        }

        private string _pendingReviewOrderNumber;
        private string _pendingReviewPhone;

        private sealed class ReviewPhotoUploadResult
        {
            public string Url { get; set; }
        }

        /// <summary>
        /// Lấy danh sách đánh giá đơn hàng cho admin.
        /// </summary>
        /// <returns>Danh sách đánh giá.</returns>
        public async Task<List<OrderReviewModel>> GetReviewsAsync()
        {
            return await GetAsync<List<OrderReviewModel>>("api/orders/reviews");
        }

        // ---- Support tickets ----

        /// <summary>
        /// Tạo ticket hỗ trợ khách hàng.
        /// </summary>
        /// <param name="ticket">Thông tin ticket cần tạo.</param>
        /// <returns>Ticket vừa tạo.</returns>
        public async Task<SupportTicketModel> CreateTicketAsync(CreateSupportTicketModel ticket)
        {
            return await PostAsync<SupportTicketModel>("api/supporttickets", ticket);
        }

        /// <summary>
        /// Lấy danh sách ticket hỗ trợ, lọc theo trạng thái (bỏ trống là lấy tất cả).
        /// </summary>
        /// <param name="status">Trạng thái cần lọc; null/rỗng để lấy tất cả.</param>
        /// <returns>Danh sách ticket.</returns>
        public async Task<List<SupportTicketModel>> GetTicketsAsync(string status)
        {
            var url = "api/supporttickets";
            if (!string.IsNullOrWhiteSpace(status))
            {
                url += $"?status={Uri.EscapeDataString(status)}";
            }

            return await GetAsync<List<SupportTicketModel>>(url);
        }

        /// <summary>
        /// Nhân viên trả lời ticket và cập nhật trạng thái ticket.
        /// </summary>
        /// <param name="id">Id ticket.</param>
        /// <param name="replyNote">Nội dung trả lời.</param>
        /// <param name="status">Trạng thái mới của ticket.</param>
        /// <returns>Ticket sau khi trả lời.</returns>
        public async Task<SupportTicketModel> ReplyTicketAsync(Guid id, string replyNote, string status)
        {
            return await PutAsync<SupportTicketModel>($"api/supporttickets/{id}/reply",
                new { ReplyNote = replyNote, Status = status });
        }

        // ---- FAQ ----

        /// <summary>
        /// Lấy danh sách FAQ công khai, hỗ trợ tìm kiếm theo từ khóa.
        /// </summary>
        /// <param name="searchTerm">Từ khóa tìm kiếm; bỏ trống để lấy tất cả.</param>
        /// <returns>Danh sách câu hỏi thường gặp.</returns>
        public async Task<List<FaqItemModel>> GetFaqsAsync(string searchTerm)
        {
            var url = "api/faqs";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"?searchTerm={Uri.EscapeDataString(searchTerm)}";
            }

            return await GetAsync<List<FaqItemModel>>(url);
        }

        /// <summary>
        /// Lưu 1 mục FAQ (tạo mới khi Id rỗng, cập nhật khi đã có Id).
        /// </summary>
        public async Task<FaqItemModel> SaveFaqItemAsync(FaqItemModel faqItem)
        {
            if (faqItem.Id == Guid.Empty)
            {
                return await PostAsync<FaqItemModel>("api/faqs", faqItem);
            }

            return await PutAsync<FaqItemModel>($"api/faqs/{faqItem.Id}", faqItem);
        }

        /// <summary>
        /// Xóa một mục FAQ (admin).
        /// </summary>
        /// <param name="id">Id mục FAQ cần xóa.</param>
        public async Task DeleteFaqAsync(Guid id)
        {
            await DeleteAsync($"api/faqs/{id}");
        }

        // ---- Subscriptions ----

        /// <summary>
        /// Tạo gói đăng ký (subscription) cho khách.
        /// </summary>
        /// <param name="subscription">Thông tin gói đăng ký.</param>
        /// <returns>Gói đăng ký vừa tạo.</returns>
        public async Task<SubscriptionModel> CreateSubscriptionAsync(CreateSubscriptionModel subscription)
        {
            return await PostAsync<SubscriptionModel>("api/subscriptions", subscription);
        }

        /// <summary>
        /// Lấy danh sách gói đăng ký; activeOnly=true chỉ lấy gói đang hoạt động.
        /// </summary>
        /// <param name="activeOnly">true để chỉ lấy gói đang hoạt động, null/false để lấy tất cả.</param>
        /// <returns>Danh sách gói đăng ký.</returns>
        public async Task<List<SubscriptionModel>> GetSubscriptionsAsync(bool? activeOnly)
        {
            var url = "api/subscriptions";
            if (activeOnly.HasValue)
            {
                url += $"?activeOnly={activeOnly.Value.ToString().ToLowerInvariant()}";
            }

            return await GetAsync<List<SubscriptionModel>>(url);
        }

        /// <summary>
        /// Bật/tắt một gói đăng ký của khách.
        /// </summary>
        /// <param name="id">Id gói đăng ký.</param>
        /// <param name="active">true để kích hoạt, false để tạm ngưng.</param>
        /// <returns>Gói đăng ký sau khi cập nhật.</returns>
        public async Task<SubscriptionModel> SetSubscriptionActiveAsync(Guid id, bool active)
        {
            return await PutAsync<SubscriptionModel>($"api/subscriptions/{id}/active",
                new { Active = active });
        }

        // ---- Integrations ----

        /// <summary>
        /// Lấy trạng thái kết nối Zalo OA (đã cấu hình/chưa, token còn hạn không).
        /// </summary>
        /// <returns>Trạng thái tích hợp Zalo.</returns>
        public async Task<ZaloStatusModel> GetZaloStatusAsync()
        {
            return await GetAsync<ZaloStatusModel>("api/integrations/zalo");
        }

        /// <summary>
        /// Gửi tin nhắn Zalo test tới một SĐT để kiểm tra tích hợp.
        /// </summary>
        /// <param name="phone">SĐT nhận tin nhắn test.</param>
        /// <param name="message">Nội dung tin nhắn test.</param>
        public async Task TestZaloAsync(string phone, string message)
        {
            await PostAsync<object>("api/integrations/zalo/test",
                new { Phone = phone, Message = message });
        }
    }
}
