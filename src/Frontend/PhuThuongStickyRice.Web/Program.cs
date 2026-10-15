using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Dashboard.Services;
using PhuThuongStickyRice.Web.Components.Landing.Services;
using PhuThuongStickyRice.Web.Components.Orders.Services;
using PhuThuongStickyRice.Web.Components.Products.Services;
using PhuThuongStickyRice.Web.Components.Shop.Services;
using PhuThuongStickyRice.Web.Components.Admin.Services;
using PhuThuongStickyRice.Web.ConfigurationOptions;
using PhuThuongStickyRice.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web
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

            // Wave 6d (item 37): change-password (any authenticated user).
            builder.Services.AddHttpClient<AuthService, AuthService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Customer account: saved addresses (authenticated).
            builder.Services.AddHttpClient<CustomerAddressService, CustomerAddressService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Wave 6b (item 28): policy pages (public GET, admin PUT via JWT).
            builder.Services.AddHttpClient<PolicyService, PolicyService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Shop commerce settings (public GET, admin PUT via JWT).
            builder.Services.AddHttpClient<ShopSettingsService, ShopSettingsService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Admin: discount codes (authenticated).
            builder.Services.AddHttpClient<DiscountService, DiscountService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Admin: staff accounts & roles (authenticated, Admin only).
            builder.Services.AddHttpClient<StaffService, StaffService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Admin: catalog features (toppings, combos, quotes, dish votes, size options).
            builder.Services.AddHttpClient<CatalogAdminService, CatalogAdminService>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Wave 2: loyalty, spin, support, FAQ, subscriptions, reviews, integrations.
            builder.Services.AddHttpClient<Wave2Service, Wave2Service>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Wave 3: shippers, blacklist, CRM notes, promos, reward rules,
            // articles, table QRs, profit report, login audit, 2FA.
            builder.Services.AddHttpClient<Wave3Service, Wave3Service>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Wave 4: feature flags, branches, webhooks, short links, app
            // logs, disk usage, payment config, group orders, address book.
            builder.Services.AddHttpClient<Wave4Service, Wave4Service>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Wave 5: check-in, corporate clients, gallery, forecast, shifts,
            // wallet, reconciliation, refunds, templates, campaigns, POS.
            builder.Services.AddHttpClient<Wave5Service, Wave5Service>(client =>
            {
                client.BaseAddress = new Uri(appSettings.ResourceServer.Endpoint);
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 134: VI/EN dictionary for the shop pages' chrome.
            builder.Services.AddScoped<PhuThuongStickyRice.Web.Components.Shop.Services.ShopI18n>();

            // Toast notifications.
            builder.Services.AddScoped<ToastService>();

            await builder.Build().RunAsync();
        }
    }
}
