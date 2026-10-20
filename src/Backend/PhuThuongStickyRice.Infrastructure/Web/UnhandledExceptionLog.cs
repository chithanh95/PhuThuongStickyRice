using Microsoft.Extensions.Logging;
using System;

namespace PhuThuongStickyRice.Infrastructure.Web
{
    // [codequality-smell 2026-10-20] Gom LoggerMessage delegate LogUnhandledException
    // bị duplicate ở GlobalExceptionFilter và GlobalExceptionHandlerMiddleware
    // về 1 helper duy nhất — đổi template/correlation 1 nơi, cả 2 luồng giữ đồng nhất.
    /// <summary>
    /// Ghi log exception chưa xử lý (lỗi 500) dùng chung cho exception filter và
    /// exception middleware. Template biên dịch sẵn (CA1848) — message render ra
    /// "[{Ticks}-{ThreadId}]" kèm exception đầy đủ để truy vết.
    /// </summary>
    internal static class UnhandledExceptionLog
    {
        // CA1848: dùng LoggerMessage delegate thay LoggerExtensions.LogError —
        // template biên dịch sẵn, không parse lại mỗi lần log. Nội dung và
        // exception kèm theo giữ nguyên.
        private static readonly Action<ILogger, long, int, Exception> LogUnhandledException =
            LoggerMessage.Define<long, int>(
                LogLevel.Error,
                new EventId(1, "UnhandledException"),
                "[{Ticks}-{ThreadId}]");

        /// <summary>
        /// Ghi log 1 exception chưa xử lý kèm correlation id (ticks) và thread id.
        /// </summary>
        /// <param name="logger">Logger của caller.</param>
        /// <param name="exception">Exception chưa được xử lý.</param>
        internal static void Log(ILogger logger, Exception exception) =>
            LogUnhandledException(logger, DateTime.UtcNow.Ticks, Environment.CurrentManagedThreadId, exception);
    }
}
