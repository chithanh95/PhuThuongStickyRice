using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PhuThuongStickyRice.Application.Facebook
{
    public interface IFacebookPostService : IApplicationService
    {
        /// <summary>
        /// Publishes a product announcement to the Facebook Page feed.
        /// When the Page ID / Page token is not configured, the attempt is
        /// logged as Skipped and never throws.
        /// </summary>
        Task<FacebookPublishResultDto> PublishProductAsync(Guid productId);

        /// <summary>Whether auto-posting is configured.</summary>
        Task<FacebookStatusDto> GetStatusAsync();

        /// <summary>Recent auto-post history, newest first.</summary>
        Task<List<FacebookPostLogDto>> GetHistoryAsync(int maxCount = 50);
    }

    /// <summary>
    /// Item 130: auto-post new products to the Facebook Page feed.
    ///
    /// Setup (Facebook App dashboard):
    ///   1. Create an App at developers.facebook.com, add the Page's product
    ///      (or "Facebook Login for Business"), link the Fanpage.
    ///   2. Generate a Page Access Token with the <c>pages_manage_posts</c>
    ///      permission -&gt; paste into Admin &gt; Cài đặt cửa hàng &gt;
    ///      Chatbot Messenger &gt; Page Access Token (the same token is
    ///      reused for the chatbot and for auto-posting).
    ///   3. Find the Page ID (Fanpage &gt; About &gt; Page ID) -&gt; paste it
    ///      into Admin &gt; Cài đặt cửa hàng &gt; Facebook Page ID.
    ///
    /// Without a Page ID + Page token the service runs in "waiting for
    /// configuration" mode: attempts are logged as Skipped, nothing throws.
    /// </summary>
    public class FacebookPostService : ApplicationService, IFacebookPostService
    {
        private const string GraphApiVersion = "v18.0";

        private readonly IShopSettingAppService _shopSettings;
        private readonly IProductRepository _products;
        private readonly Volo.Abp.Domain.Repositories.IRepository<FacebookPostLog, Guid> _logs;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FacebookPostService> _logger;

        public FacebookPostService(
            IShopSettingAppService shopSettings,
            IProductRepository products,
            Volo.Abp.Domain.Repositories.IRepository<FacebookPostLog, Guid> logs,
            IHttpClientFactory httpClientFactory,
            ILogger<FacebookPostService> logger)
        {
            _shopSettings = shopSettings;
            _products = products;
            _logs = logs;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<FacebookStatusDto> GetStatusAsync()
        {
            var settings = await _shopSettings.GetAsync();
            var configured = !string.IsNullOrWhiteSpace(settings?.FacebookPageId)
                && !string.IsNullOrWhiteSpace(settings?.FacebookPageAccessToken);
            return new FacebookStatusDto
            {
                Configured = configured,
                PageId = settings?.FacebookPageId
            };
        }

        public async Task<List<FacebookPostLogDto>> GetHistoryAsync(int maxCount = 50)
        {
            var query = await _logs.GetQueryableAsync();
            var items = query
                .OrderByDescending(x => x.CreatedAt)
                .Take(Math.Min(Math.Max(maxCount, 1), 200))
                .ToList();
            return items.Select(x => new FacebookPostLogDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                PostUrl = x.PostUrl,
                Status = x.Status,
                ErrorMessage = x.ErrorMessage,
                CreatedAt = x.CreatedAt
            }).ToList();
        }

        public async Task<FacebookPublishResultDto> PublishProductAsync(Guid productId)
        {
            var product = await _products.GetAsync(productId);
            var settings = await _shopSettings.GetAsync();
            var pageId = settings?.FacebookPageId?.Trim();
            var pageToken = settings?.FacebookPageAccessToken;

            if (string.IsNullOrWhiteSpace(pageId) || string.IsNullOrWhiteSpace(pageToken))
            {
                await SaveLogAsync(product.Id, product.Name, null,
                    FacebookPostLog.StatusSkipped,
                    "Chưa cấu hình Facebook Page ID / Page Access Token.");
                _logger.LogInformation(
                    "[Facebook: chưa cấu hình Page ID/token] bỏ qua đăng món {ProductName}.",
                    product.Name);
                return new FacebookPublishResultDto
                {
                    Status = FacebookPostLog.StatusSkipped,
                    Message = "Chưa cấu hình Facebook (thiếu Page ID hoặc Page Access Token ở Cài đặt cửa hàng). Bài đăng đã được bỏ qua, không có lỗi."
                };
            }

            var price = product.Price.Amount;
            var unit = string.Equals(product.Unit, "kg", StringComparison.OrdinalIgnoreCase) ? "kg" : "suất";
            var shopUrl = (settings.WebBaseUrl ?? string.Empty).TrimEnd('/');
            var message = $"🍚 MÓN MỚI — {product.Name}\n" +
                          $"💰 Giá: {price:N0}₫/{unit}\n" +
                          (!string.IsNullOrWhiteSpace(product.Description) ? $"{product.Description.Trim()}\n" : string.Empty) +
                          "👉 Đặt ngay tại web Xôi Phú Thượng!" +
                          (string.IsNullOrWhiteSpace(shopUrl) ? string.Empty : $"\n{shopUrl}/shop");

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);
                var url = $"https://graph.facebook.com/{GraphApiVersion}/{pageId}/feed";
                var form = new Dictionary<string, string>
                {
                    ["message"] = message,
                    ["access_token"] = pageToken
                };
                if (!string.IsNullOrWhiteSpace(shopUrl))
                {
                    form["link"] = shopUrl + "/shop";
                }
                using var content = new FormUrlEncodedContent(form);
                using var resp = await client.PostAsync(url, content);
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    var error = $"Facebook trả về {(int)resp.StatusCode}: {Truncate(body, 300)}";
                    await SaveLogAsync(product.Id, product.Name, null,
                        FacebookPostLog.StatusFailed, error);
                    _logger.LogWarning("Đăng Facebook thất bại cho món {ProductName}: {Error}",
                        product.Name, error);
                    return new FacebookPublishResultDto
                    {
                        Status = FacebookPostLog.StatusFailed,
                        Message = "Đăng lên Facebook thất bại: " + Truncate(body, 200)
                    };
                }

                string postId = null;
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("id", out var idProp))
                    {
                        postId = idProp.GetString();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Không đọc được post id từ phản hồi Facebook.");
                }

                var postUrl = string.IsNullOrWhiteSpace(postId)
                    ? null
                    : $"https://www.facebook.com/{postId}";
                await SaveLogAsync(product.Id, product.Name, postUrl,
                    FacebookPostLog.StatusPosted, null);
                _logger.LogInformation("Đã đăng món {ProductName} lên Facebook: {PostUrl}",
                    product.Name, postUrl);
                return new FacebookPublishResultDto
                {
                    Status = FacebookPostLog.StatusPosted,
                    PostUrl = postUrl,
                    Message = "Đã đăng lên Facebook thành công!"
                };
            }
            catch (Exception ex)
            {
                var error = Truncate(ex.Message, 300);
                await SaveLogAsync(product.Id, product.Name, null,
                    FacebookPostLog.StatusFailed, error);
                _logger.LogWarning(ex, "Lỗi khi đăng món {ProductName} lên Facebook.", product.Name);
                return new FacebookPublishResultDto
                {
                    Status = FacebookPostLog.StatusFailed,
                    Message = "Lỗi khi đăng lên Facebook: " + Truncate(ex.Message, 200)
                };
            }
        }

        private async Task SaveLogAsync(Guid? productId, string productName,
            string postUrl, string status, string errorMessage)
        {
            await _logs.InsertAsync(new FacebookPostLog
            {
                ProductId = productId,
                ProductName = productName,
                PostUrl = postUrl,
                Status = status,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            }, autoSave: true);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }
            return value.Substring(0, maxLength) + "...";
        }
    }
}
