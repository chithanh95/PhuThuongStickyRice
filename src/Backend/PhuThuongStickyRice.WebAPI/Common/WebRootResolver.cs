using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;

namespace PhuThuongStickyRice.WebAPI.Common
{
    /// <summary>
    /// Helper dùng chung để lấy thư mục wwwroot: ưu tiên
    /// <see cref="IWebHostEnvironment.WebRootPath"/>, fallback về thư mục
    /// "wwwroot" cạnh file chạy khi env null (trường hợp self-host/test).
    /// Gom 1 chỗ để 4 nơi dùng (workers + controller) không lặp code.
    /// </summary>
    internal static class WebRootResolver
    {
        public static string Resolve(IWebHostEnvironment? env)
            => env?.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
    }
}
