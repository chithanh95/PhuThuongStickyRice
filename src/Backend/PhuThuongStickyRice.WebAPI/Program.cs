using PhuThuongStickyRice.Infrastructure.Logging;
using PhuThuongStickyRice.WebAPI.ConfigurationOptions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PhuThuongStickyRice.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UsePhuThuongStickyRiceLogger(configuration =>
                {
                    var appSettings = new AppSettings();
                    configuration.Bind(appSettings);
                    return appSettings.Logging;
                });
    }
}
