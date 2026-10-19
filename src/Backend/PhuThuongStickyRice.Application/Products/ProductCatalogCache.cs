using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Threading;
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
    /// hàng loạt). Transient nhưng SemaphoreSlim chống stampede là static vì
    /// IMemoryCache dùng chung toàn process. Nằm trên đường nóng của checkout
    /// (EvaluateBestAsync chạy mỗi lần xem giỏ) nên mọi logic validate áp dụng
    /// mã vẫn kiểm tra lại server-side — cache hết hạn/chậm tối đa 5 phút là
    /// chấp nhận được vì validate sẽ từ chối mã đã hết hiệu lực.
    /// </summary>
    public class ProductCatalogCache : IProductCatalogCache
    {
        private const string VersionKey = "product-catalog:version";
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Khóa per-key chống cache stampede (thundering herd): khi 1 key nóng
        /// hết TTL, chỉ 1 request chạy factory (query DB), các request đồng thời
        /// còn lại chờ rồi đọc kết quả từ cache. Static vì class này là transient
        /// (mỗi lần resolve 1 instance mới), còn IMemoryCache thì dùng chung toàn
        /// process. Semaphore được dọn sau khi dùng để không phình dictionary.
        /// </summary>
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

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

            var semaphore = Locks.GetOrAdd(versionedKey, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                // Double-check: request khác có thể đã nạp cache trong lúc mình chờ khóa.
                if (_cache.TryGetValue(versionedKey, out T cachedAfterLock))
                {
                    return cachedAfterLock;
                }

                var value = await factory();
                _cache.Set(versionedKey, value, Ttl);
                return value;
            }
            finally
            {
                semaphore.Release();
                Locks.TryRemove(versionedKey, out _);
            }
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
