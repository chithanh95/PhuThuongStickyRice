using PhuThuongStickyRice.Blazor.Modules.Core.Services;
using PhuThuongStickyRice.Blazor.Modules.Dashboard.Services;
using PhuThuongStickyRice.Blazor.Modules.Landing.Services;
using PhuThuongStickyRice.Blazor.Modules.Orders.Services;
using PhuThuongStickyRice.Blazor.Modules.Products.Services;
using PhuThuongStickyRice.Blazor.Modules.Shop.Services;
using PhuThuongStickyRice.BlazorWebAssembly.ConfigurationOptions;
using PhuThuongStickyRice.BlazorWebAssembly.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.BlazorWebAssembly
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            var appSettings = new AppSettings();
            builder.Configuration.Bind(appSettings);
            builder.Services.AddSingleton(appSettings);

            // In-repo JWT admin login (no OIDC / external IdentityServer).
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<JwtTokenManager>();
            builder.Services.AddScoped<JwtAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
                sp.GetRequiredService<JwtAuthenticationStateProvider>());
            builder.Services.AddScoped<ITokenManager>(sp =>
                sp.GetRequiredService<JwtTokenManager>());

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            // Admin services attach the JWT via HttpService + ITokenManager.
            builder.Services.AddHttpClient<ProductService, ProductService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Public storefront (anonymous).
            builder.Services.AddHttpClient<ShopService, ShopService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });
            builder.Services.AddScoped<CartState>();

            // Admin: orders + dashboard (authenticated).
            builder.Services.AddHttpClient<OrderService, OrderService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            builder.Services.AddHttpClient<DashboardService, DashboardService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Landing page content (public GET, admin PUT via JWT).
            builder.Services.AddHttpClient<LandingService, LandingService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            await builder.Build().RunAsync();
        }
    }
}
