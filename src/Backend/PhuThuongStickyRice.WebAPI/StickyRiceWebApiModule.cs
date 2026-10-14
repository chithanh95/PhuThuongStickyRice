using PhuThuongStickyRice.Application;
using PhuThuongStickyRice.Persistence;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace PhuThuongStickyRice.WebAPI
{
    [DependsOn(
        typeof(StickyRiceApplicationModule),
        typeof(StickyRiceEntityFrameworkCoreModule),
        typeof(AbpAspNetCoreMvcModule),
        typeof(AbpAutofacModule))]
    public class StickyRiceWebApiModule : AbpModule
    {
    }
}
