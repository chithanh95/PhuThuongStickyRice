using PhuThuongStickyRice.Infrastructure.HealthChecks;
using DbUp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.Reflection;

namespace PhuThuongStickyRice.Migrator
{
    /// <summary>
    /// Cấu hình của Migrator — tiến trình riêng chuyên chạy migration khi deploy.
    /// <c>ConfigureServices</c> đăng ký DbContext + persistence, <c>Configure</c>
    /// áp dụng migration EF Core trước rồi chạy script SQL nhúng (DbUp) sau;
    /// thứ tự này là cố ý: schema tạo bởi EF trước, script dữ liệu chạy trên
    /// schema đã ổn định.
    /// </summary>
    public class Startup
    {
        /// <summary>Khởi tạo <c>Startup</c> với cấu hình ứng dụng.</summary>
        /// <param name="configuration">Cấu hình (appsettings.json + biến môi trường).</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>Cấu hình ứng dụng đã load.</summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Đăng ký dịch vụ: nếu bật <c>CheckDependency</c>, đợi dependency (SQL
        /// Server) mở port trước khi làm gì khác — deploy dạng container DB
        /// thường chưa sẵn sàng ngay khi Migrator khởi động.
        /// </summary>
        /// <param name="services">Service collection của host.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            if (string.Equals(Configuration["CheckDependency:Enabled"], "true", System.StringComparison.OrdinalIgnoreCase))
            {
                // Chờ host dependency mở port trước khi migrate: thử lại tối đa
                // 5 lần, mỗi lần cách nhau 30 giây (tham số mặc định của
                // NetworkPortCheck.Wait) — tránh fail ngay khi DB container
                // khởi động chậm hơn Migrator.
                NetworkPortCheck.Wait(Configuration["CheckDependency:Host"], 5);
            }

            services.AddDateTimeProvider();

            services.AddPersistence(Configuration["ConnectionStrings:PhuThuongStickyRice"],
                typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
        }

        /// <summary>
        /// Áp dụng toàn bộ thay đổi DB lúc khởi động Migrator. Đọc connection
        /// string <c>ConnectionStrings:PhuThuongStickyRice</c> — chính là DB
        /// mà WebAPI và BackgroundServer dùng.
        /// </summary>
        /// <param name="app">Application builder của host.</param>
        /// <param name="env">Môi trường host (Development/Production...).</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Retry với backoff tăng dần 10s → 20s → 30s (tổng ~60s) vì lúc
            // deploy DB có thể chưa chấp nhận kết nối (failover, container mới
            // khởi động): lỗi thoáng qua được thử lại thay vì fail ngay.
            Policy.Handle<Exception>().WaitAndRetry(new[]
            {
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromSeconds(30),
            })
            .Execute(() =>
            {
                app.MigrateStickyRiceDb();

                // DbUp chạy các script SQL nhúng trong assembly (thư mục
                // Scripts): dùng cho dữ liệu/seed mà EF migration viết bằng
                // code khó diễn đạt. Chạy SAU migration EF nên không cần quan
                // tâm thứ tự tạo bảng.
                var upgrader = DeployChanges.To
                .SqlDatabase(Configuration.GetConnectionString("PhuThuongStickyRice"))
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .LogToConsole()
                .Build();

                var result = upgrader.PerformUpgrade();

                if (!result.Successful)
                {
                    // Không nuốt lỗi migration: throw để host báo fail, pipeline
                    // deploy biết mà dừng thay vì chạy app trên schema dở dang.
                    throw result.Error;
                }
            });
        }
    }
}
