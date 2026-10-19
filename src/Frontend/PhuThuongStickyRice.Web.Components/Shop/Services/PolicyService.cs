using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Wave 6b (item 28): policy pages — public list/by-slug, admin
    /// list/update. Follows the FAQ service pattern.
    /// </summary>
    public class PolicyService : ApiClientBase
    {
        /// <summary>
        /// TTL cache client-side cho danh sách policy public: nội dung đổi hiếm
        /// khi nhưng endpoint được gọi ở footer mọi trang (landing + FAQ +
        /// trang policy). Cache nằm trên static field vì service đăng ký
        /// Transient qua AddHttpClient (mỗi page inject instance riêng) nên
        /// instance-field không chia sẻ được. 3 caller (Index/Faq/PolicyPage)
        /// chỉ đọc list (foreach, Where().ToList()) — không caller nào mutate —
        /// nên trả cùng instance là an toàn.
        /// </summary>
        private static readonly TimeSpan PublicListCacheTtl = TimeSpan.FromMinutes(2);
        private static readonly SemaphoreSlim PublicListCacheLock = new(1, 1);
        private static List<PolicyPageSummaryModel>? _cachedPublicList;
        private static DateTimeOffset _cachedPublicListAt;

        public PolicyService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// Public: slug + title of every page (footer links). Kết quả được
        /// cache client-side 2 phút (static, vì service là Transient): cắt
        /// ~1 HTTP round-trip trên mỗi trang mở mà dữ liệu không đổi.
        /// </summary>
        public async Task<List<PolicyPageSummaryModel>> GetPublicListAsync()
        {
            var cached = _cachedPublicList;
            if (cached is not null && DateTimeOffset.UtcNow - _cachedPublicListAt < PublicListCacheTtl)
                return cached;

            // Lock tránh N request cùng fetch khi cache hết hạn (chỉ 1 fetch).
            await PublicListCacheLock.WaitAsync();
            try
            {
                // Kiểm tra lại sau khi vào lock: request khác có thể đã refresh xong.
                cached = _cachedPublicList;
                if (cached is not null && DateTimeOffset.UtcNow - _cachedPublicListAt < PublicListCacheTtl)
                    return cached;

                var fresh = await GetAsync<List<PolicyPageSummaryModel>>("api/policies")
                    ?? new List<PolicyPageSummaryModel>();
                _cachedPublicList = fresh;
                _cachedPublicListAt = DateTimeOffset.UtcNow;
                return fresh;
            }
            finally
            {
                PublicListCacheLock.Release();
            }
        }

        /// <summary>Xóa cache danh sách policy public (gọi sau khi admin sửa).</summary>
        public static void InvalidatePublicListCache()
        {
            _cachedPublicList = null;
        }

        /// <summary>Public: one page by slug; null when the slug is unknown.</summary>
        public async Task<PolicyPageModel> GetBySlugAsync(string slug)
        {
            await SetBearerToken();
            // Dispose response để trả kết nối HTTP về pool — using var vẫn
            // dispose đúng cả khi return sớm ở nhánh 404 (CA2000).
            using var response = await _httpClient.GetAsync(
                $"api/policies/{Uri.EscapeDataString(slug ?? string.Empty)}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<PolicyPageModel>();
        }

        /// <summary>Admin: all pages with bodies.</summary>
        public async Task<List<PolicyPageModel>> GetAllAsync()
        {
            return await GetAsync<List<PolicyPageModel>>("api/policies/all")
                ?? new List<PolicyPageModel>();
        }

        /// <summary>Admin: update title/body/order.</summary>
        public async Task<PolicyPageModel> UpdateAsync(Guid id, PolicyPageModel model)
        {
            var result = await PutAsync<PolicyPageModel>($"api/policies/{id}", new
            {
                Title = model.Title,
                Body = model.Body,
                SortOrder = model.SortOrder
            });
            // Admin sửa xong: xóa cache public để footer thấy nội dung mới ngay.
            InvalidatePublicListCache();
            return result;
        }
    }
}
