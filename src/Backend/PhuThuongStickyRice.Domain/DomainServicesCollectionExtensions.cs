namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension đăng ký các domain service của PhuThuongStickyRice vào DI.
    /// Hiện tại domain chưa có service cần đăng ký thủ công nên method trả
    /// về nguyên services — giữ lại làm điểm mở rộng khi có domain service mới.
    /// </summary>
    public static class DomainServicesCollectionExtensions
    {
        /// <summary>
        /// Đăng ký các domain service vào <paramref name="services"/>.
        /// </summary>
        /// <returns>Cùng <paramref name="services"/> để chain fluent.</returns>
        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            return services;
        }
    }
}
