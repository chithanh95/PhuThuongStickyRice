using PhuThuongStickyRice.Infrastructure.Monitoring.AzureApplicationInsights;
using PhuThuongStickyRice.Infrastructure.Monitoring.MiniProfiler;
using PhuThuongStickyRice.Infrastructure.Monitoring.OpenTelemetry;

namespace PhuThuongStickyRice.Infrastructure.Monitoring
{
    public class MonitoringOptions
    {
        public MiniProfilerOptions MiniProfiler { get; set; }

        public AzureApplicationInsightsOptions AzureApplicationInsights { get; set; }

        public OpenTelemetryOptions OpenTelemetry { get; set; }
    }
}
