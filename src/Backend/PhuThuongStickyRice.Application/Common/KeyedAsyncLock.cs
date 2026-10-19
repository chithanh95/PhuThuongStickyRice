using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Common
{
    /// <summary>
    /// Khóa bất đồng bộ theo khóa (key): các thao tác cùng key chạy tuần tự,
    /// khác key chạy song song. Dùng để đóng race check-then-act trên cùng 1
    /// đơn hàng/SĐT (double-submit, retry mạng, 2 tab mở song song) mà không
    /// cần migration hay lock phân tán — ứng dụng chạy single instance.
    /// Cùng pattern per-key SemaphoreSlim đã dùng ở
    /// <c>ProductCatalogCache</c> (chống cache stampede).
    /// Lưu ý: key phải được chuẩn hóa (SĐT về 9 số cuối, mã đơn trim +
    /// uppercase) để 2 request cùng 1 đơn/SĐT nhưng khác format input vẫn
    /// rơi vào cùng 1 khóa.
    /// </summary>
    public static class KeyedAsyncLock
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

        /// <summary>
        /// Chạy <paramref name="action"/> dưới khóa độc quyền của
        /// <paramref name="key"/> rồi trả kết quả.
        /// </summary>
        /// <typeparam name="T">Kiểu kết quả của action.</typeparam>
        /// <param name="key">Khóa đã chuẩn hóa, xác định phạm vi serialize.</param>
        /// <param name="action">Thao tác cần chạy tuần tự theo key.</param>
        /// <returns>Kết quả của <paramref name="action"/>.</returns>
        public static async Task<T> RunAsync<T>(string key, Func<Task<T>> action)
        {
            var semaphore = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                semaphore.Release();
                // Dọn semaphore sau khi dùng để dictionary không phình theo
                // số đơn/SĐT đã qua. An toàn khi có waiter: waiter giữ reference
                // riêng, release/remove của nó là no-op.
                Locks.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Chạy <paramref name="action"/> (không trả kết quả) dưới khóa độc
        /// quyền của <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Khóa đã chuẩn hóa, xác định phạm vi serialize.</param>
        /// <param name="action">Thao tác cần chạy tuần tự theo key.</param>
        public static Task RunAsync(string key, Func<Task> action) =>
            RunAsync<object?>(key, async () =>
            {
                await action();
                return null;
            });
    }
}
