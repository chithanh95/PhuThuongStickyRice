using PhuThuongStickyRice.CrossCuttingConcerns.Exceptions;
using PhuThuongStickyRice.Infrastructure.Web;
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
            else if (context.Exception is Domain.Exceptions.DomainException)
            {
                // Lỗi nghiệp vụ (vd chuyển trạng thái đơn sai bước ở kanban):
                // là lỗi phía thao tác (400) kèm message thật cho người dùng —
                // trước đây rơi xuống nhánh 500 nên frontend chỉ thấy
                // "Internal Server Error" (user gặp 2026-10-21).
                context.Result = new BadRequestObjectResult(context.Exception.Message);
            }
            else
            {
                // Message template có parameter (thay vì $"...") để correlation id
                // thành field có cấu trúc trong log, tra cứu được — message render ra giống hệt.
                UnhandledExceptionLog.Log(_logger, context.Exception);

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
                GlobalExceptionDetailLevel.None => "Đã có lỗi xảy ra. Vui lòng thử lại.",
                GlobalExceptionDetailLevel.Message => ex.Message,
                // ex.StackTrace có thể null (vd: exception chưa từng được throw)
                // → coalesce để contract "Message luôn là chuỗi" của JSON lỗi 500
                // giữ nguyên, không trả về "Message": null.
                GlobalExceptionDetailLevel.StackTrace => ex.StackTrace ?? "Đã có lỗi xảy ra. Vui lòng thử lại.",
                GlobalExceptionDetailLevel.ToString => ex.ToString(),
                _ => "Đã có lỗi xảy ra. Vui lòng thử lại.",
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
