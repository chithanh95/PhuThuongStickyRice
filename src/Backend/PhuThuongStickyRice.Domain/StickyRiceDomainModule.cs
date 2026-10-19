using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace PhuThuongStickyRice.Domain
{
    /// <summary>
    /// ABP module của tầng Domain: điểm khởi động DDD (entity, value object,
    /// repository interface, domain event) của PhuThuongStickyRice.
    /// </summary>
    [DependsOn(typeof(AbpDddDomainModule))]
    public class StickyRiceDomainModule : AbpModule
    {
    }
}
