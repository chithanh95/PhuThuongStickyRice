using PhuThuongStickyRice.BackgroundServer.ConfigurationOptions;
using PhuThuongStickyRice.BackgroundServer.HostedServices;
using PhuThuongStickyRice.BackgroundServer.Identity;
using PhuThuongStickyRice.Domain.Identity;
using PhuThuongStickyRice.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace PhuThuongStickyRice.BackgroundServer
{
    /// <summary>
    /// Điểm vào của BackgroundServer: tiến trình chạy nền độc lập, thực thi
    /// các worker nền (gửi email trong hàng đợi, báo cáo
    /// tuần cho chủ quán) thay vì đặt timer trong WebAPI — để WebAPI restart
    /// hay scale cũng không làm mất việc nền.
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
        /// Dựng host: dùng WindowsService (chạy như service trên máy quán),
        /// logging tập trung, validate cấu hình lúc khởi động (sai config thì
        /// dừng ngay thay vì chạy trong trạng thái hỏng), rồi đăng ký
        /// persistence/domain/application và 2 worker nền.
        /// </summary>
        /// <param name="args">Tham số dòng lệnh.</param>
        /// <returns>Builder đã cấu hình đầy đủ để <c>Build().Run()</c>.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .UsePhuThuongStickyRiceLogger(configuration =>
            {
                var appSettings = new AppSettings();
                configuration.Bind(appSettings);
                return appSettings.Logging;
            })
            .ConfigureServices((hostContext, services) =>
            {
                // ASP0000: không gọi services.BuildServiceProvider() trong
                // ConfigureServices — tạo thêm 1 bản copy singleton (IConfiguration,
                // options...) ngoài container chính của host. Dùng trực tiếp
                // hostContext.Configuration (cùng instance IConfiguration mà
                // host đăng ký), hành vi đọc config giữ nguyên.
                var configuration = hostContext.Configuration;

                var appSettings = new AppSettings();
                configuration.Bind(appSettings);

                var validationResult = AppSettings.Validate();
                if (validationResult.Failed)
                {
                    // CA2201: config sai lúc khởi động là lỗi vận hành — ném
                    // InvalidOperationException thay vì Exception chung chung.
                    throw new InvalidOperationException(validationResult.FailureMessage);
                }

                services.Configure<AppSettings>(configuration);

                services.AddScoped<ICurrentUser, CurrentUser>();

                services.AddDateTimeProvider();
                services.AddPersistence(appSettings.ConnectionStrings.PhuThuongStickyRice)
                        .AddDomainServices()
                        .AddApplicationServices();

                services.AddNotificationServices(appSettings.Notification);

                services.AddHostedService<SendEmailWorker>();
                services.AddHostedService<WeeklyReportWorker>();
            });
    }
}
