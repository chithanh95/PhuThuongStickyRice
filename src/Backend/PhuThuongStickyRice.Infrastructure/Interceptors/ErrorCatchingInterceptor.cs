using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Interceptors
{
    public class ErrorCatchingInterceptor : IInterceptor
    {
        // CA1869: JsonSerializerOptions đắt khi khởi tạo (build metadata cache mỗi lần
        // new) — dùng 1 instance static dùng chung, thread-safe sau khi cấu hình xong.
        private static readonly JsonSerializerOptions LogArgumentsOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };

        private readonly ILogger _logger;

        public ErrorCatchingInterceptor(ILogger<ErrorCatchingInterceptor> logger)
        {
            _logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            try
            {
                var returnType = invocation.Method.ReturnType;
                if (returnType == typeof(Task))
                {
                    invocation.Proceed();
                    invocation.ReturnValue = InterceptResultAsync((dynamic)invocation.ReturnValue, invocation);
                }
                else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    invocation.Proceed();
                    invocation.ReturnValue = InterceptResultAsync((dynamic)invocation.ReturnValue, invocation);
                }
                else
                {
                    invocation.Proceed();
                }
            }
            catch (Exception ex)
            {
                LogException(invocation, ex);
                throw;
            }
        }

        private async Task InterceptResultAsync(Task task, IInvocation invocation)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogException(invocation, ex);
                throw;
            }
        }

        private async Task<T> InterceptResultAsync<T>(Task<T> task, IInvocation invocation)
        {
            try
            {
                T result = await task.ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                LogException(invocation, ex);
                throw;
            }
        }

        private void LogException(IInvocation invocation, Exception ex)
        {
            var method = invocation.Method;
            var className = method.DeclaringType.Name;
            var methodName = method.Name;

            // TODO: Ignore serialize large argument object.
            // FIX: x.GetType() ném NullReferenceException khi argument là null —
            // đúng lúc đang log exception thì interceptor tự crash và che mất lỗi gốc.
            // Pattern "is not" null-safe (null không phải CancellationToken nên vẫn được serialize).
            var arguments = JsonSerializer.Serialize(
                invocation.Arguments.Where(x => x is not CancellationToken), LogArgumentsOptions);

            // FIX: dùng message template thay vì string interpolation — tránh cấp phát
            // chuỗi khi log level tắt, và truyền ex đúng cách để giữ stack trace có cấu trúc.
            _logger.LogError(ex,
                "An unhandled exception has occurred while executing the method: {ClassName}.{MethodName} with ({Arguments}).",
                className, methodName, arguments);
        }
    }
}
