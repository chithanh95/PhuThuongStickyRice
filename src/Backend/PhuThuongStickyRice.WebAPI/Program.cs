using PhuThuongStickyRice.Infrastructure.Logging;
using PhuThuongStickyRice.WebAPI.ConfigurationOptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PhuThuongStickyRice.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Generic Host + Autofac (ABP standard): ABP's interception
            // (UnitOfWork, Auditing, Validation, Authorization on Application
            // Services) is implemented with Castle DynamicProxy and only works
            // with Autofac, not with the built-in MS DI container.
            Host.CreateDefaultBuilder(args)
                .UseAutofac()
                .ConfigureWebHostDefaults(webBuilder => webBuilder
                    .UseStartup<Startup>()
                    .UsePhuThuongStickyRiceLogger(configuration =>
                    {
                        var appSettings = new AppSettings();
                        configuration.Bind(appSettings);
                        return appSettings.Logging;
                    }))
                .Build()
                .Run();
        }
    }
}
