using PhuThuongStickyRice.Domain;
using Volo.Abp.Application;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace PhuThuongStickyRice.Application
{
    [DependsOn(
        typeof(StickyRiceDomainModule),
        typeof(AbpDddApplicationModule),
        typeof(AbpAutoMapperModule))]
    /// <summary>
    /// Module ABP tầng application: phụ thuộc Domain + DDD application,
    /// đăng ký profile AutoMapper của layer này.
    /// </summary>
    public class StickyRiceApplicationModule : AbpModule
    {
        /// <summary>Đăng ký AutoMapper scan từ module này.</summary>
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options => options.AddMaps<StickyRiceApplicationModule>());
        }
    }
}
