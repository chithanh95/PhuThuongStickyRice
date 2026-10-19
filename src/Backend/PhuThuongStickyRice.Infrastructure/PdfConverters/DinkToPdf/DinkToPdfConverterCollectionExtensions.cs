using PhuThuongStickyRice.CrossCuttingConcerns.PdfConverter;
using PhuThuongStickyRice.Infrastructure.PdfConverters.DinkToPdf;
using DinkToPdf;
using DinkToPdf.Contracts;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DinkToPdfConverterCollectionExtensions
    {
        /// <summary>
        /// Đăng ký bộ chuyển đổi HTML sang PDF (DinkToPdf) vào DI.
        /// </summary>
        public static IServiceCollection AddDinkToPdfConverter(this IServiceCollection services)
        {
            // CA2263: dùng overload generic AddSingleton{TService}(instance)
            // thay cho AddSingleton(typeof, instance) — cùng hành vi.
            services.AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()));
            services.AddSingleton<IPdfConverter, DinkToPdfConverter>();

            return services;
        }
    }
}
