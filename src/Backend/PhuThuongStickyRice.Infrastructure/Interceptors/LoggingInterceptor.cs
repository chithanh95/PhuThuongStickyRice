using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Interceptors
{
    public class LoggingInterceptor : IInterceptor
    {
        // CA1869: JsonSerializerOptions đắt khi khởi tạo — dùng 1 instance static
        // dùng chung, thread-safe sau khi cấu hình xong.
        private static readonly JsonSerializerOptions LogArgumentsOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };

        private readonly ILogger _logger;

        // CA1848: interceptor chạy trên MỌI method call được intercept — dùng
        // LoggerMessage delegate thay LogDebug nội suy chuỗi để không cấp phát
        // chuỗi log khi level Debug đang tắt. Nội dung log giữ nguyên.
        private static readonly Action<ILogger, string, string, string, Exception> LogMethodStart =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Debug,
                new EventId(1, "MethodStart"),
                "Start calling method: {ClassName}.{MethodName} with ({Arguments}).");

        private static readonly Action<ILogger, string, string, long, Exception> LogMethodFinished =
            LoggerMessage.Define<string, string, long>(
                LogLevel.Debug,
                new EventId(2, "MethodFinished"),
                "Finished calling method: {ClassName}.{MethodName}. Took: {ElapsedMs} milliseconds");

        public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Chặn mọi service call đã đăng ký interceptor: log Debug khi bắt đầu/kết thúc.
        /// Lưu ý perf: chỉ serialize arguments sang JSON khi Debug log đang bật —
        /// ở production (Information trở lên) việc serialize này chạy trên MỌI call
        /// nhưng kết quả bị vứt đi, lãng phí CPU/GC trên hot path.
        /// </summary>
        public void Intercept(IInvocation invocation)
        {
            var method = invocation.Method;
            var className = method.DeclaringType.Name;
            var methodName = method.Name;

            // Chỉ serialize arguments khi Debug log đang bật — tránh JSON serialization
            // vô ích trên mọi service call ở production. Dùng pattern null-safe
            // "x is not CancellationToken" như ErrorCatchingInterceptor (x.GetType()
            // cũ ném NullReferenceException khi argument là null, che mất lỗi gốc).
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var arguments = JsonSerializer.Serialize(
                    invocation.Arguments.Where(x => x is not CancellationToken),
                    LogArgumentsOptions);

                LogMethodStart(_logger, className, methodName, arguments, null);
            }

            var watch = new Stopwatch();
            watch.Start();

            var returnType = invocation.Method.ReturnType;
            if (returnType == typeof(Task))
            {
                invocation.Proceed();
                invocation.ReturnValue = InterceptResultAsync((dynamic)invocation.ReturnValue, watch, className, methodName);
            }
            else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                invocation.Proceed();
                invocation.ReturnValue = InterceptResultAsync((dynamic)invocation.ReturnValue, watch, className, methodName);
            }
            else
            {
                invocation.Proceed();
                InterceptResult(invocation.ReturnValue, watch, className, methodName);
            }
        }

        private void InterceptResult(object returnValue, Stopwatch watch, string className, string methodName)
        {
            watch.Stop();

            LogMethodFinished(_logger, className, methodName, watch.ElapsedMilliseconds, null);
        }

        private async Task InterceptResultAsync(Task task, Stopwatch watch, string className, string methodName)
        {
            await task.ConfigureAwait(false);

            watch.Stop();

            LogMethodFinished(_logger, className, methodName, watch.ElapsedMilliseconds, null);
        }

        private async Task<T> InterceptResultAsync<T>(Task<T> task, Stopwatch watch, string className, string methodName)
        {
            T result = await task.ConfigureAwait(false);

            watch.Stop();

            LogMethodFinished(_logger, className, methodName, watch.ElapsedMilliseconds, null);

            return result;
        }
    }
}
