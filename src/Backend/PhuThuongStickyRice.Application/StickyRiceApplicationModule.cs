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
    public class StickyRiceApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options => options.AddMaps<StickyRiceApplicationModule>());
        }
    }
}
