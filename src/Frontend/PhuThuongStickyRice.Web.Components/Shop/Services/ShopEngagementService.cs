using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Shop customer-engagement APIs: loyalty, lucky spin, order
    /// self-service, reviews, support tickets, FAQ, subscriptions
    /// and third-party integrations.
    /// </summary>
    public class ShopEngagementService : HttpService
    {
        public ShopEngagementService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        // ---- Loyalty ----

        public async Task<LoyaltyAccountModel> GetLoyaltyByPhoneAsync(string phone)
        {
            var response = await _httpClient.GetAsync(
                $"api/loyalty/by-phone?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<LoyaltyAccountModel>();
        }

        public async Task<LoyaltyAccountModel> UpdateBirthdayAsync(string phone, DateTime? dateOfBirth)
        {
            return await PutAsync<LoyaltyAccountModel>(
                $"api/loyalty/by-phone/birthday?phone={Uri.EscapeDataString(phone ?? string.Empty)}",
                new { DateOfBirth = dateOfBirth });
        }

        public async Task<List<LoyaltyAccountModel>> GetLoyaltyListAsync(string searchTerm)
        {
            var url = "api/loyalty";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"?searchTerm={Uri.EscapeDataString(searchTerm)}";
            }

            return await GetAsync<List<LoyaltyAccountModel>>(url);
        }

        public async Task<LoyaltyAccountModel> AdjustPointsAsync(string phone, int delta, string reason)
        {
            return await PutAsync<LoyaltyAccountModel>(
                $"api/loyalty/{Uri.EscapeDataString(phone)}/adjust",
                new AdjustPointsModel { PointsDelta = delta, Reason = reason });
        }

        public async Task<List<LoyaltyAccountModel>> GetUpcomingBirthdaysAsync(int daysAhead)
        {
            return await GetAsync<List<LoyaltyAccountModel>>($"api/loyalty/birthdays?daysAhead={daysAhead}");
        }

        // ---- Lucky spin ----

        public async Task<SpinResultModel> SpinAsync(Guid orderId, string phone)
        {
            return await PostAsync<SpinResultModel>("api/orders/spin",
                new { OrderId = orderId, CustomerPhone = phone });
        }

        // ---- Order self-service ----

        public async Task<TrackedOrderModel> CancelOrderByCustomerAsync(string orderNumber, string phone)
        {
            return await PostAsync<TrackedOrderModel>("api/orders/track/cancel",
                new { OrderNumber = orderNumber, CustomerPhone = phone });
        }

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
        public async Task<string> UploadReviewPhotoAsync(Stream content, string fileName, string contentType)
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(_pendingReviewOrderNumber ?? string.Empty), "orderNumber");
            form.Add(new StringContent(_pendingReviewPhone ?? string.Empty), "customerPhone");

            var response = await _httpClient.PostAsync("api/orders/track/review/photo", form);
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

        public async Task<List<OrderReviewModel>> GetReviewsAsync()
        {
            return await GetAsync<List<OrderReviewModel>>("api/orders/reviews");
        }

        // ---- Support tickets ----

        public async Task<SupportTicketModel> CreateTicketAsync(CreateSupportTicketModel ticket)
        {
            return await PostAsync<SupportTicketModel>("api/supporttickets", ticket);
        }

        public async Task<List<SupportTicketModel>> GetTicketsAsync(string status)
        {
            var url = "api/supporttickets";
            if (!string.IsNullOrWhiteSpace(status))
            {
                url += $"?status={Uri.EscapeDataString(status)}";
            }

            return await GetAsync<List<SupportTicketModel>>(url);
        }

        public async Task<SupportTicketModel> ReplyTicketAsync(Guid id, string replyNote, string status)
        {
            return await PutAsync<SupportTicketModel>($"api/supporttickets/{id}/reply",
                new { ReplyNote = replyNote, Status = status });
        }

        // ---- FAQ ----

        public async Task<List<FaqItemModel>> GetFaqsAsync(string searchTerm)
        {
            var url = "api/faqs";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"?searchTerm={Uri.EscapeDataString(searchTerm)}";
            }

            return await GetAsync<List<FaqItemModel>>(url);
        }

        public async Task<List<FaqItemModel>> GetAllFaqsAsync()
        {
            return await GetAsync<List<FaqItemModel>>("api/faqs/all");
        }

        public async Task<FaqItemModel> SaveFaqAsync(FaqItemModel faq)
        {
            if (faq.Id == Guid.Empty)
            {
                return await PostAsync<FaqItemModel>("api/faqs", faq);
            }

            return await PutAsync<FaqItemModel>($"api/faqs/{faq.Id}", faq);
        }

        public async Task DeleteFaqAsync(Guid id)
        {
            await DeleteAsync($"api/faqs/{id}");
        }

        // ---- Subscriptions ----

        public async Task<SubscriptionModel> CreateSubscriptionAsync(CreateSubscriptionModel subscription)
        {
            return await PostAsync<SubscriptionModel>("api/subscriptions", subscription);
        }

        public async Task<List<SubscriptionModel>> GetSubscriptionsAsync(bool? activeOnly)
        {
            var url = "api/subscriptions";
            if (activeOnly.HasValue)
            {
                url += $"?activeOnly={activeOnly.Value.ToString().ToLowerInvariant()}";
            }

            return await GetAsync<List<SubscriptionModel>>(url);
        }

        public async Task<SubscriptionModel> SetSubscriptionActiveAsync(Guid id, bool active)
        {
            return await PutAsync<SubscriptionModel>($"api/subscriptions/{id}/active",
                new { Active = active });
        }

        // ---- Integrations ----

        public async Task<ZaloStatusModel> GetZaloStatusAsync()
        {
            return await GetAsync<ZaloStatusModel>("api/integrations/zalo");
        }

        public async Task TestZaloAsync(string phone, string message)
        {
            await PostAsync<object>("api/integrations/zalo/test",
                new { Phone = phone, Message = message });
        }
    }
}
