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
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory);

        void Invalidate();
    }

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

        public ProductCatalogCache(IMemoryCache cache)
        {
            _cache = cache;
        }

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

        public void Invalidate()
        {
            _cache.Set(VersionKey, GetVersion() + 1, TimeSpan.FromHours(1));
        }

        private int GetVersion()
        {
            return _cache.TryGetValue<int>(VersionKey, out var version) ? version : 0;
        }
    }
}
