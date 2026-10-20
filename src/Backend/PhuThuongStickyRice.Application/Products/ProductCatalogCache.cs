using Microsoft.Extensions.Caching.Memory;
using PhuThuongStickyRice.Application.Common;
using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PhuThuongStickyRice.Application.Products
{
    /// <summary>
    /// Public product-catalog cache (item 101). Versioned-key scheme:
    /// <see cref="Invalidate"/> bumps the version so every cached catalog
    /// entry is stale at once (IMemoryCache has no bulk eviction).
    /// The public shop list is cached for 5 minutes.
    /// </summary>
    public interface IProductCatalogCache : ITransientDependency
    {
        /// <summary>
        /// Lấy giá trị từ cache, chưa có thì chạy <paramref name="factory"/>
        /// (query DB) rồi nạp vào cache. Chống cache stampede: các request
        /// đồng thời cùng key chỉ có 1 bên chạy factory.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu cache.</typeparam>
        /// <param name="key">Khóa cache (chưa gồm version).</param>
        /// <param name="factory">Hàm nạp dữ liệu khi cache miss.</param>
        /// <returns>Dữ liệu từ cache hoặc vừa nạp.</returns>
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory);

        /// <summary>
        /// Vô hiệu hóa toàn bộ cache catalog: tăng version nên mọi entry cũ
        /// thành stale ngay lập tức. Gọi sau mỗi thao tác ghi (tạo/sửa/xóa
        /// sản phẩm, mã giảm giá...) để shop thấy dữ liệu mới.
        /// </summary>
        void Invalidate();
    }

    /// <summary>
    /// Cache danh sách catalog public (sản phẩm, mã giảm giá công khai...) bằng
    /// IMemoryCache với TTL 5 phút. Dùng versioned-key: <see cref="Invalidate"/>
    /// chỉ tăng version thay vì xóa từng entry (IMemoryCache không hỗ trợ xóa
    /// hàng loạt). Transient nhưng khóa per-key chống stampede dùng
    /// <see cref="KeyedAsyncLock"/> (refcount + dispose đúng lúc, không race
    /// như pattern TryRemove trực tiếp). Nằm trên đường nóng của checkout
    /// (EvaluateBestDiscountAsync chạy mỗi lần xem giỏ) nên mọi logic validate áp dụng
    /// mã vẫn kiểm tra lại server-side — cache hết hạn/chậm tối đa 5 phút là
    /// chấp nhận được vì validate sẽ từ chối mã đã hết hiệu lực.
    /// </summary>
    public class ProductCatalogCache : IProductCatalogCache
    {
        private const string VersionKey = "product-catalog:version";
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

        private readonly IMemoryCache _cache;

        /// <summary>Khởi tạo cache catalog với IMemoryCache dùng chung.</summary>
        /// <param name="cache">Bộ nhớ cache dùng chung toàn process.</param>
        public ProductCatalogCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Lấy giá trị từ cache theo khóa đã gắn version; cache miss thì 1
        /// request duy nhất chạy factory (double-check sau khi lấy khóa để
        /// request chờ không chạy factory thừa), các request còn lại chờ rồi
        /// đọc kết quả.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu cache.</typeparam>
        /// <param name="key">Khóa cache (chưa gồm version).</param>
        /// <param name="factory">Hàm nạp dữ liệu khi cache miss.</param>
        /// <returns>Dữ liệu từ cache hoặc vừa nạp.</returns>
        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
        {
            var versionedKey = $"{key}:v{GetVersion()}";
            if (_cache.TryGetValue(versionedKey, out T cached))
            {
                return cached;
            }

            // [codequality-practices 2026-10-20] Dùng KeyedAsyncLock chung thay
            // cho ConcurrentDictionary<string, SemaphoreSlim> + TryRemove trực
            // tiếp: bản cũ có race giữa Release và TryRemove — request mới
            // GetOrAdd sau TryRemove tạo semaphore mới trong khi request đang
            // chờ vẫn giữ semaphore cũ → 2 bên cùng chạy factory (stampede tái
            // diễn); semaphore cũ cũng không được Dispose (rò handle OS).
            // KeyedAsyncLock đếm RefCount nên chỉ xóa entry khi không còn ai
            // giữ/chờ và Dispose đúng lúc — mutual exclusion không bao giờ gãy.
            // Ngữ nghĩa giữ nguyên: double-check sau khi lấy khóa, 1 factory
            // duy nhất chạy cho mỗi versioned key.
            return await KeyedAsyncLock.RunAsync(versionedKey, async () =>
            {
                // Double-check: request khác có thể đã nạp cache trong lúc mình chờ khóa.
                if (_cache.TryGetValue(versionedKey, out T cachedAfterLock))
                {
                    return cachedAfterLock;
                }

                var value = await factory();
                _cache.Set(versionedKey, value, Ttl);
                return value;
            });
        }

        /// <summary>
        /// Vô hiệu hóa toàn bộ cache: tăng version lên 1 nên mọi khóa versioned
        /// cũ thành stale ngay lập tức; version lưu 1 giờ để không bị reset
        /// giữa chừng (reset về 0 sẽ làm entry cũ sống lại).
        /// </summary>
        public void Invalidate()
        {
            _cache.Set(VersionKey, GetVersion() + 1, TimeSpan.FromHours(1));
        }

        /// <summary>Đọc version cache hiện tại; chưa có thì coi như 0.</summary>
        private int GetVersion()
        {
            return _cache.TryGetValue<int>(VersionKey, out var version) ? version : 0;
        }
    }
}
