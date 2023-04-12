using Serilog.Events;

namespace PhuThuongStickyRice.Infrastructure.Logging
{
    public class ElasticsearchOptions
    {
        public bool IsEnabled { get; set; }

        public string Host { get; set; }

        public string IndexFormat { get; set; }

        public LogEventLevel MinimumLogEventLevel { get; set; }
    }
}
