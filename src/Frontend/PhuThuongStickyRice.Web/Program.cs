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
        /// <summary>
        /// Đăng ký typed HttpClient trỏ về API backend cho một service.
        /// Config chung cho mọi service: BaseAddress = API backend,
        /// timeout 30 giây, header mặc định đã clear.
        /// </summary>
        private static void AddApiHttpClient<TService>(WebAssemblyHostBuilder builder, Uri apiBaseUri)
            where TService : class
        {
            builder.Services.AddHttpClient<TService, TService>(client =>
            {
                client.BaseAddress = apiBaseUri;
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Clear();
            });
        }

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
            builder.Services.AddScoped<ITokenStore>(sp =>
                sp.GetRequiredService<JwtTokenStore>());

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            // Admin services attach the JWT via ApiClientBase + ITokenStore.
            AddApiHttpClient<ProductService>(builder, apiBaseUri);

            // Item 185: admin quản lý gói "Ăn sáng cả tuần" (JWT).
            AddApiHttpClient<WeeklyPlanAdminService>(builder, apiBaseUri);

            // Public storefront (anonymous).
            AddApiHttpClient<ShopService>(builder, apiBaseUri);

            // Item 185: gói "Ăn sáng cả tuần" (public).
            AddApiHttpClient<WeeklyPlanService>(builder, apiBaseUri);
            builder.Services.AddScoped<CartState>();

            // Item 137: cart sync for logged-in users.
            AddApiHttpClient<CartSyncService>(builder, apiBaseUri);

            // Admin: orders + dashboard (authenticated).
            AddApiHttpClient<OrderService>(builder, apiBaseUri);

            // Item 130 (đúng): VIP marking + priority.
            AddApiHttpClient<VipCustomerService>(builder, apiBaseUri);

            AddApiHttpClient<DashboardService>(builder, apiBaseUri);

            // Landing page content (public GET, admin PUT via JWT).
            AddApiHttpClient<LandingService>(builder, apiBaseUri);

            // Wave 6d (item 37): change-password (any authenticated user).
            AddApiHttpClient<AuthService>(builder, apiBaseUri);

            // Customer account: saved addresses (authenticated).
            AddApiHttpClient<CustomerAddressService>(builder, apiBaseUri);

            // Item 90+92: customer profile (taste preferences, birthday).
            AddApiHttpClient<CustomerProfileService>(builder, apiBaseUri);

            // Item 91+92: admin customer care (reminders, birthday gifts).
            AddApiHttpClient<CustomerCareService>(builder, apiBaseUri);

            // Wave 6b (item 28): policy pages (public GET, admin PUT via JWT).
            AddApiHttpClient<PolicyService>(builder, apiBaseUri);

            // Shop commerce settings (public GET, admin PUT via JWT).
            AddApiHttpClient<ShopSettingsService>(builder, apiBaseUri);

            // Item 117: holiday schedule (public upcoming + admin CRUD).
            AddApiHttpClient<HolidayService>(builder, apiBaseUri);

            // Item 178: public queue-number display (/cho, no login).
            AddApiHttpClient<QueueTicketService>(builder, apiBaseUri);

            // Admin: discount codes (authenticated).
            AddApiHttpClient<DiscountService>(builder, apiBaseUri);

            // Admin: staff accounts & roles (authenticated, Admin only).
            AddApiHttpClient<StaffService>(builder, apiBaseUri);

            // Items 155/156: fine-grained permissions + staff activity log.
            AddApiHttpClient<PermissionService>(builder, apiBaseUri);
            AddApiHttpClient<StaffActivityLogService>(builder, apiBaseUri);

            // Admin: catalog features (toppings, combos, quotes, dish votes, size options).
            AddApiHttpClient<CatalogAdminService>(builder, apiBaseUri);

            // Shop engagement: loyalty, spin, support, FAQ, subscriptions, reviews, integrations.
            AddApiHttpClient<ShopEngagementService>(builder, apiBaseUri);

            // Item 164: prepaid cards (shop buy + lookup).
            AddApiHttpClient<PrepaidCardService>(builder, apiBaseUri);

            // Item 164: prepaid cards (admin list + redeem).
            AddApiHttpClient<PrepaidCardAdminService>(builder, apiBaseUri);

            // Admin operations: shippers, blacklist, CRM notes, promos, reward rules,
            // articles, table QRs, profit report, login audit, 2FA.
            AddApiHttpClient<AdminOperationsService>(builder, apiBaseUri);

            // Admin system: feature flags, branches, webhooks, short links, app
            // logs, disk usage, payment config, group orders, address book.
            AddApiHttpClient<AdminSystemService>(builder, apiBaseUri);

            // Admin commerce: check-in, corporate clients, gallery, forecast, shifts,
            // wallet, reconciliation, refunds, templates, campaigns, POS.
            AddApiHttpClient<AdminCommerceService>(builder, apiBaseUri);

            // Item 134: VI/EN dictionary for the shop pages' chrome.
            builder.Services.AddScoped<PhuThuongStickyRice.Web.Components.Shop.Services.ShopI18n>();

            // App-wide i18n (VI default, EN/ZH/JA) for the public web chrome.
            builder.Services.AddScoped<PhuThuongStickyRice.Web.Components.Core.Services.AppI18n>();

            // GL1: anonymous funnel measurement.
            AddApiHttpClient<PhuThuongStickyRice.Web.Components.Shop.Services.FunnelService>(builder, apiBaseUri);

            // Item 87: end-of-day inventory counts (kiểm kê cuối ngày).
            AddApiHttpClient<PhuThuongStickyRice.Web.Components.Admin.Services.InventoryService>(builder, apiBaseUri);

            // Items 157–158: admin global search + notification center.
            AddApiHttpClient<PhuThuongStickyRice.Web.Components.Admin.Services.AdminSearchNotificationService>(builder, apiBaseUri);

            // Item 162: order message templates (mẫu tin nhắn theo trạng thái đơn).
            AddApiHttpClient<PhuThuongStickyRice.Web.Components.Admin.Services.OrderMessageTemplateService>(builder, apiBaseUri);

            // Toast notifications.
            builder.Services.AddScoped<ToastService>();

            await builder.Build().RunAsync();
        }
    }
}
