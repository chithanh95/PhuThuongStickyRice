using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace PhuThuongStickyRice.Domain
{
    [DependsOn(typeof(AbpDddDomainModule))]
    public class StickyRiceDomainModule : AbpModule
    {
    }
}
