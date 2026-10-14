using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Landing.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Landing.Services
{
    public class LandingService : HttpService
    {
        public LandingService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<LandingContentModel> GetContentAsync()
        {
            return await GetAsync<LandingContentModel>("api/landing/content");
        }

        /// <summary>Item 93: published news articles for the landing page.</summary>
        public async Task<List<ArticleModel>> GetArticlesAsync()
        {
            return await GetAsync<List<ArticleModel>>("api/articles/published");
        }

        /// <summary>Item 87: public non-hidden order reviews for the landing page.</summary>
        public async Task<List<PublicReviewModel>> GetPublicReviewsAsync()
        {
            return await GetAsync<List<PublicReviewModel>>("api/orders/reviews/public");
        }

        public async Task<LandingContentModel> UpdateContentAsync(LandingContentModel model)
        {
            return await PutAsync<LandingContentModel>("api/landing/content", model);
        }

        /// <summary>
        /// Uploads one image and returns its relative URL (e.g. /uploads/landing/xxx.jpg).
        /// Throws <see cref="InvalidOperationException"/> with the server message on validation failure.
        /// </summary>
        public async Task<string> UploadImageAsync(Stream content, string fileName, string contentType)
        {
            await SetBearerToken();

            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);

            var response = await _httpClient.PostAsync("api/landing/images", form);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var message = "Tải ảnh lên thất bại.";
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var msg))
                    {
                        message = msg.GetString() ?? message;
                    }
                }
                catch (JsonException)
                {
                }

                throw new InvalidOperationException(message);
            }

            using var result = JsonDocument.Parse(body);
            if (result.RootElement.TryGetProperty("url", out var url))
            {
                return url.GetString();
            }

            throw new InvalidOperationException("Máy chủ không trả về đường dẫn ảnh.");
        }

        /// <summary>
        /// Builds the absolute URL for an image returned by the upload endpoint.
        /// The API serves uploads from its own wwwroot, so relative URLs like
        /// /uploads/landing/xxx.jpg are resolved against the API base address.
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
    }
}
