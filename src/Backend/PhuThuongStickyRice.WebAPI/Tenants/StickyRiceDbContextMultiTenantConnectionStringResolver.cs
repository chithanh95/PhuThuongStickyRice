using PhuThuongStickyRice.CrossCuttingConcerns.Tenants;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.WebAPI.ConfigurationOptions;
using Microsoft.Extensions.Options;

namespace PhuThuongStickyRice.WebAPI.Tenants
{
    /// <summary>
    /// Resolver connection string cho DbContext multi-tenant: ưu tiên connection
    /// string riêng của tenant, fallback về connection string mặc định trong
    /// appsettings khi chưa xác định được tenant. Hiện tại mọi request đều dùng
    /// chung DB mặc định (xem TODO trong <c>ConnectionString</c>).
    /// </summary>
    public class StickyRiceDbContextMultiTenantConnectionStringResolver : IConnectionStringResolver<StickyRiceDbContextMultiTenant>
    {
        private readonly AppSettings _appSettings;
        private readonly ITenantResolver _tenantResolver;

        /// <summary>
        /// Khởi tạo resolver connection string cho multi-tenant DbContext.
        /// </summary>
        /// <param name="appSettings">
        /// Snapshot appsettings (đọc lại mỗi request) chứa connection string mặc định.
        /// </param>
        /// <param name="tenantResolver">Resolver xác định tenant của request hiện tại.</param>
        public StickyRiceDbContextMultiTenantConnectionStringResolver(IOptionsSnapshot<AppSettings> appSettings,
            ITenantResolver tenantResolver)
        {
            _appSettings = appSettings.Value;
            _tenantResolver = tenantResolver;
        }

        /// <summary>
        /// Connection string cho request hiện tại: của tenant nếu xác định được,
        /// ngược lại là connection string mặc định <c>PhuThuongStickyRice</c>.
        /// </summary>
        public string ConnectionString
        {
            get
            {
                var tenant = _tenantResolver.Tenant;
                if (tenant is null)
                {
                    return _appSettings.ConnectionStrings.PhuThuongStickyRice;
                }

                // TODO: query configuration store to get ConnectionString
                // Chưa có kho cấu hình ánh xạ tenant → connection string riêng,
                // nên kể cả khi có tenant vẫn dùng chung DB mặc định.
                return _appSettings.ConnectionStrings.PhuThuongStickyRice;
            }
        }

        /// <summary>
        /// Assembly chứa migration của DbContext này — hiện để rỗng vì dùng chung
        /// migration với DbContext chính (không có migration riêng cho tenant).
        /// </summary>
        public string MigrationsAssembly => string.Empty;
    }
}
