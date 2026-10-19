using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace PhuThuongStickyRice.Migrator
{
    /// <summary>
    /// Điểm vào của Migrator: công cụ console chạy migration EF Core và seed
    /// dữ liệu ban đầu. Chạy thủ công (hoặc <c>Update-Database</c> trong PMC)
    /// mỗi khi có migration mới, thay vì để WebAPI tự migrate lúc khởi động —
    /// tránh race khi nhiều instance cùng migrate và giữ quyền kiểm soát
    /// thời điểm đổi schema.
    /// </summary>
    public class Program
    {
        /// <summary>Khởi động host và chạy đến khi bị dừng.</summary>
        /// <param name="args">Tham số dòng lệnh.</param>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Dựng host web mặc định với <c>Startup</c> — Startup chứa endpoint
        /// apply migration và các bước seed, gọi qua HTTP hoặc chạy trực tiếp.
        /// </summary>
        /// <param name="args">Tham số dòng lệnh.</param>
        /// <returns>Builder đã cấu hình đầy đủ để <c>Build().Run()</c>.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
