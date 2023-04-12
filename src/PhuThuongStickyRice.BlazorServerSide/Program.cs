using PhuThuongStickyRice.BlazorServerSide.ConfigurationOptions;
using PhuThuongStickyRice.Infrastructure.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace PhuThuongStickyRice.BlazorServerSide
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UsePhuThuongStickyRiceLogger(configuration =>
                     {
                         var appSettings = new AppSettings();
                         configuration.Bind(appSettings);
                         return appSettings.Logging;
                     });
                });
    }
}
