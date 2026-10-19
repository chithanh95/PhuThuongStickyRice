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

            // API endpoint:
            // - Dev (localhost): dùng config (Web và API khác port).
            // - LAN/deploy (không phải localhost): API và Web chung origin -> dùng same-origin.
            var baseAddress = builder.HostEnvironment.BaseAddress;
            var isLocalhost = baseAddress.Contains("localhost", System.StringComparison.OrdinalIgnoreCase)
                || baseAddress.Contains("127.0.0.1");
            var apiEndpoint = isLocalhost
                ? appSettings.ResourceServer?.Endpoint?.Trim()
                : baseAddress.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(apiEndpoint) || !System.Uri.TryCreate(apiEndpoint, System.UriKind.Absolute, out _))
            {
                apiEndpoint = baseAddress.TrimEnd('/');
            }
            var apiBaseUri = new System.Uri(apiEndpoint);

            // In-repo JWT admin login (no OIDC / external IdentityServer).
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<JwtTokenStore>();
            builder.Services.AddScoped<JwtAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
                sp.GetRequiredService<JwtAuthenticationStateProvider>());
            builder.Services.AddScoped<ITokenManager>(sp =>
                sp.GetRequiredService<JwtTokenStore>());

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            // Admin services attach the JWT via HttpService + ITokenManager.
            builder.Services.AddHttpClient<ProductService, ProductService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 185: admin quản lý gói "Ăn sáng cả tuần" (JWT).
            builder.Services.AddHttpClient<WeeklyPlanAdminService, WeeklyPlanAdminService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Public storefront (anonymous).
            builder.Services.AddHttpClient<ShopService, ShopService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 185: gói "Ăn sáng cả tuần" (public).
            builder.Services.AddHttpClient<WeeklyPlanService, WeeklyPlanService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });
            builder.Services.AddScoped<CartState>();

            // Item 137: cart sync for logged-in users.
            builder.Services.AddHttpClient<CartSyncService, CartSyncService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Admin: orders + dashboard (authenticated).
            builder.Services.AddHttpClient<OrderService, OrderService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 130 (đúng): VIP marking + priority.
            builder.Services.AddHttpClient<VipService, VipService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            builder.Services.AddHttpClient<DashboardService, DashboardService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Landing page content (public GET, admin PUT via JWT).
            builder.Services.AddHttpClient<LandingService, LandingService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Wave 6d (item 37): change-password (any authenticated user).
            builder.Services.AddHttpClient<AuthService, AuthService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Customer account: saved addresses (authenticated).
            builder.Services.AddHttpClient<CustomerAddressService, CustomerAddressService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 90+92: customer profile (taste preferences, birthday).
            builder.Services.AddHttpClient<CustomerProfileService, CustomerProfileService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 91+92: admin customer care (reminders, birthday gifts).
            builder.Services.AddHttpClient<CustomerCareService, CustomerCareService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Wave 6b (item 28): policy pages (public GET, admin PUT via JWT).
            builder.Services.AddHttpClient<PolicyService, PolicyService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Shop commerce settings (public GET, admin PUT via JWT).
            builder.Services.AddHttpClient<ShopSettingsService, ShopSettingsService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 117: holiday schedule (public upcoming + admin CRUD).
            builder.Services.AddHttpClient<HolidayService, HolidayService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 178: public queue-number display (/cho, no login).
            builder.Services.AddHttpClient<QueueService, QueueService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Admin: discount codes (authenticated).
            builder.Services.AddHttpClient<DiscountService, DiscountService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Admin: staff accounts & roles (authenticated, Admin only).
            builder.Services.AddHttpClient<StaffService, StaffService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Items 155/156: fine-grained permissions + staff activity log.
            builder.Services.AddHttpClient<PermissionService, PermissionService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });
            builder.Services.AddHttpClient<StaffActivityLogService, StaffActivityLogService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Admin: catalog features (toppings, combos, quotes, dish votes, size options).
            builder.Services.AddHttpClient<CatalogAdminService, CatalogAdminService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Shop engagement: loyalty, spin, support, FAQ, subscriptions, reviews, integrations.
            builder.Services.AddHttpClient<ShopEngagementService, ShopEngagementService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 164: prepaid cards (shop buy + lookup).
            builder.Services.AddHttpClient<PrepaidCardService, PrepaidCardService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 164: prepaid cards (admin list + redeem).
            builder.Services.AddHttpClient<PrepaidCardAdminService, PrepaidCardAdminService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Admin operations: shippers, blacklist, CRM notes, promos, reward rules,
            // articles, table QRs, profit report, login audit, 2FA.
            builder.Services.AddHttpClient<AdminOperationsService, AdminOperationsService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Admin system: feature flags, branches, webhooks, short links, app
            // logs, disk usage, payment config, group orders, address book.
            builder.Services.AddHttpClient<AdminSystemService, AdminSystemService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Admin commerce: check-in, corporate clients, gallery, forecast, shifts,
            // wallet, reconciliation, refunds, templates, campaigns, POS.
            builder.Services.AddHttpClient<AdminCommerceService, AdminCommerceService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 134: VI/EN dictionary for the shop pages' chrome.
            builder.Services.AddScoped<PhuThuongStickyRice.Web.Components.Shop.Services.ShopI18n>();

            // App-wide i18n (VI default, EN/ZH/JA) for the public web chrome.
            builder.Services.AddScoped<PhuThuongStickyRice.Web.Components.Core.Services.AppI18n>();

            // GL1: anonymous funnel measurement.
            builder.Services.AddHttpClient<PhuThuongStickyRice.Web.Components.Shop.Services.FunnelService, PhuThuongStickyRice.Web.Components.Shop.Services.FunnelService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 87: end-of-day inventory counts (kiểm kê cuối ngày).
            builder.Services.AddHttpClient<PhuThuongStickyRice.Web.Components.Admin.Services.InventoryService, PhuThuongStickyRice.Web.Components.Admin.Services.InventoryService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Items 157–158: admin global search + notification center.
            builder.Services.AddHttpClient<AdminUtilityService, AdminUtilityService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Item 162: order message templates (mẫu tin nhắn theo trạng thái đơn).
            builder.Services.AddHttpClient<PhuThuongStickyRice.Web.Components.Admin.Services.MessageTemplateService, PhuThuongStickyRice.Web.Components.Admin.Services.MessageTemplateService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = new TimeSpan(0, 0, 30);
                client.DefaultRequestHeaders.Clear();
            });

            // Toast notifications.
            builder.Services.AddScoped<ToastService>();

            await builder.Build().RunAsync();
        }
    }
}
