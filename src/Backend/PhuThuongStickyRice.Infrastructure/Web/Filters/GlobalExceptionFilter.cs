using PhuThuongStickyRice.CrossCuttingConcerns.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;

namespace PhuThuongStickyRice.Infrastructure.Web.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;
        private readonly GlobalExceptionFilterOptions _options;

        // CA1848: dùng LoggerMessage delegate thay LoggerExtensions.LogError —
        // template biên dịch sẵn, không parse lại mỗi lần log. Nội dung và
        // exception kèm theo giữ nguyên.
        private static readonly Action<ILogger, long, int, Exception> LogUnhandledException =
            LoggerMessage.Define<long, int>(
                LogLevel.Error,
                new EventId(1, "UnhandledException"),
                "[{Ticks}-{ThreadId}]");

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger,
            IOptionsSnapshot<GlobalExceptionFilterOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is NotFoundException)
            {
                context.Result = new NotFoundResult();
            }
            else if (context.Exception is ValidationException)
            {
                context.Result = new BadRequestObjectResult(context.Exception.Message);
            }
            else
            {
                // Message template có parameter (thay vì $"...") để correlation id
                // thành field có cấu trúc trong log, tra cứu được — message render ra giống hệt.
                LogUnhandledException(_logger, DateTime.UtcNow.Ticks, Environment.CurrentManagedThreadId, context.Exception);

                if (_options.DetailLevel == GlobalExceptionDetailLevel.Throw)
                {
                    return;
                }

                context.Result = new ObjectResult(new { Message = GetErrorMessage(context.Exception) })
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                };
            }
        }

        private string GetErrorMessage(Exception ex)
        {
            return _options.DetailLevel switch
            {
                GlobalExceptionDetailLevel.None => "An internal exception has occurred.",
                GlobalExceptionDetailLevel.Message => ex.Message,
                // ex.StackTrace có thể null (vd: exception chưa từng được throw)
                // → coalesce để contract "Message luôn là chuỗi" của JSON lỗi 500
                // giữ nguyên, không trả về "Message": null.
                GlobalExceptionDetailLevel.StackTrace => ex.StackTrace ?? "An internal exception has occurred.",
                GlobalExceptionDetailLevel.ToString => ex.ToString(),
                _ => "An internal exception has occurred.",
            };
        }
    }

    public class GlobalExceptionFilterOptions
    {
        public GlobalExceptionDetailLevel DetailLevel { get; set; }
    }

    public enum GlobalExceptionDetailLevel
    {
        None,
        Message,
        StackTrace,
        ToString,
        Throw,
    }
}
