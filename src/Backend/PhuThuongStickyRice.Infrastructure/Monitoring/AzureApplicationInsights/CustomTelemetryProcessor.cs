using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace PhuThuongStickyRice.Infrastructure.Monitoring.AzureApplicationInsights
{
    public class CustomTelemetryProcessor : ITelemetryProcessor
    {
        private ITelemetryProcessor Next { get; set; }

        public CustomTelemetryProcessor(ITelemetryProcessor next)
        {
            Next = next;
        }

        public void Process(ITelemetry item)
        {
            if (!OKtoSend(item))
            {
                return;
            }

            Next.Process(item);
        }

        // CA1822: không dùng dữ liệu instance nào nên đánh dấu static —
        // tránh tạo delegate/capture không cần thiết khi gọi từ Process.
        private static bool OKtoSend(ITelemetry item)
        {
            if (item is RequestTelemetry request)
            {
            }

            if (item is DependencyTelemetry dependency)
            {
            }

            return true;
        }
    }
}
