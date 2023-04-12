using PhuThuongStickyRice.CrossCuttingConcerns.Tenants;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.WebAPI.ConfigurationOptions;
using Microsoft.Extensions.Options;

namespace PhuThuongStickyRice.WebAPI.Tenants
{
    public class StickyRiceDbContextMultiTenantConnectionStringResolver : IConnectionStringResolver<StickyRiceDbContextMultiTenant>
    {
        private readonly AppSettings _appSettings;
        private readonly ITenantResolver _tenantResolver;

        public StickyRiceDbContextMultiTenantConnectionStringResolver(IOptionsSnapshot<AppSettings> appSettings,
            ITenantResolver tenantResolver)
        {
            _appSettings = appSettings.Value;
            _tenantResolver = tenantResolver;
        }

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
                return _appSettings.ConnectionStrings.PhuThuongStickyRice;
            }
        }

        public string MigrationsAssembly => string.Empty;
    }
}
