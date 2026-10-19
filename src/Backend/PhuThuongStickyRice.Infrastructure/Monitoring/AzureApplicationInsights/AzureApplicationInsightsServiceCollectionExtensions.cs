using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;

namespace PhuThuongStickyRice.Infrastructure.Monitoring.AzureApplicationInsights
{
    public static class AzureApplicationInsightsServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureApplicationInsights(this IServiceCollection services, AzureApplicationInsightsOptions azureApplicationInsightsOptions = null)
        {
            if (azureApplicationInsightsOptions?.IsEnabled ?? false)
            {
                services.AddApplicationInsightsTelemetry(opt =>
                {
                    // InstrumentationKey đã deprecated — SDK khuyên dùng
                    // ConnectionString. Dựng "InstrumentationKey=<key>" giữ
                    // nguyên endpoint mặc định, tương đương hành vi gán key
                    // cũ; bỏ qua khi key trống (như trước đây gán null).
                    if (!string.IsNullOrEmpty(azureApplicationInsightsOptions.InstrumentationKey))
                    {
                        opt.ConnectionString =
                            $"InstrumentationKey={azureApplicationInsightsOptions.InstrumentationKey}";
                    }
                });

                services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
                {
                    module.EnableSqlCommandTextInstrumentation = azureApplicationInsightsOptions.EnableSqlCommandTextInstrumentation;
                });

                services.AddApplicationInsightsTelemetryProcessor<CustomTelemetryProcessor>();
                services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
            }

            return services;
        }
    }
}
