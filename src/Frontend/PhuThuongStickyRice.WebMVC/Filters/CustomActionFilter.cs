using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace PhuThuongStickyRice.WebMVC.Filters
{
    public class CustomActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Items["CustomActionFilter"] = Stopwatch.StartNew();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            Stopwatch stopwatch = (Stopwatch)context.HttpContext.Items["CustomActionFilter"];
            var timeElapsed = stopwatch.Elapsed;
            context.HttpContext.Response.Headers.Add("Custom-Action-Filter", timeElapsed.TotalSeconds.ToString());
        }
    }
}
