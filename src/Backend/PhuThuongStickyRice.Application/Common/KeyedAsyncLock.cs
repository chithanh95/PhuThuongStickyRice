#nullable enable
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
        /// <summary>
        /// Entry trong dictionary: semaphore khóa theo key + bộ đếm số luồng
        /// đang giữ hoặc đang chờ. Entry chỉ bị xóa khỏi dictionary khi
        /// <see cref="RefCount"/> về 0 — tức không còn luồng nào giữ hay chờ
        /// semaphore này.
        /// </summary>
        private sealed class LockEntry : IDisposable
        {
            public readonly SemaphoreSlim Semaphore = new(1, 1);
            public int RefCount;

            /// <summary>
            /// Giải phóng semaphore khi entry bị loại khỏi dictionary
            /// (CA1001: type sở hữu field IDisposable phải disposable; lúc
            /// dispose RefCount == 0 nên không còn luồng nào giữ hay chờ).
            /// </summary>
            public void Dispose() => Semaphore.Dispose();
        }

        private static readonly ConcurrentDictionary<string, LockEntry> Entries = new();

        /// <summary>
        /// Gate chỉ bảo vệ cặp GetOrAdd/Tăng-RefCount và Giảm-RefCount/Xóa
        /// (vài chục ns, không giữ qua await) nên không gây nghẽn dù là lock
        /// toàn cục duy nhất.
        /// </summary>
        private static readonly object Gate = new();

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
            LockEntry entry;
            lock (Gate)
            {
                entry = Entries.GetOrAdd(key, _ => new LockEntry());
                // Tăng trước khi WaitAsync: luồng mới đến sau khi entry bị
                // xóa khỏi dictionary sẽ tạo entry mới tinh, không bao giờ
                // chạm vào semaphore cũ — mutual exclusion không bao giờ gãy.
                entry.RefCount++;
            }

            await entry.Semaphore.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                entry.Semaphore.Release();
                lock (Gate)
                {
                    entry.RefCount--;
                    // Chỉ xóa khi không còn ai giữ/chờ: quyết định xóa và
                    // thao tác xóa là nguyên tử dưới Gate, nên luồng mới đến
                    // hoặc thấy entry (tăng RefCount → không xóa) hoặc đến
                    // sau khi xóa (tạo entry mới). Không có cửa sổ "xóa rồi
                    // tạo mới" để 2 luồng cùng vào critical section như bản
                    // TryRemove trực tiếp trước đây.
                    if (entry.RefCount == 0 && Entries.TryRemove(key, out _))
                    {
                        // Không còn luồng nào giữ/chờ semaphore này — giải
                        // phóng handle OS ngay thay vì chờ GC (CA1001).
                        entry.Dispose();
                    }
                }
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
