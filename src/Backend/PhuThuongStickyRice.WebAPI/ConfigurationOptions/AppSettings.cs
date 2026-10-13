using PhuThuongStickyRice.Infrastructure.Caching;
using PhuThuongStickyRice.Infrastructure.Interceptors;
using PhuThuongStickyRice.Infrastructure.Logging;
using PhuThuongStickyRice.Infrastructure.Monitoring;
using PhuThuongStickyRice.Infrastructure.Storages;
using System.Collections.Generic;

namespace PhuThuongStickyRice.WebAPI.ConfigurationOptions
{
    public class AppSettings
    {
        public ConnectionStrings ConnectionStrings { get; set; }

        public LoggingOptions Logging { get; set; }

        public CachingOptions Caching { get; set; }

        public MonitoringOptions Monitoring { get; set; }

        public IdentityServerAuthentication IdentityServerAuthentication { get; set; }

        public string AllowedHosts { get; set; }

        public CORS CORS { get; set; }

        public StorageOptions Storage { get; set; }

        public Dictionary<string, string> SecurityHeaders { get; set; }

        public InterceptorsOptions Interceptors { get; set; }

        public CertificatesOptions Certificates { get; set; }
    }
}
