using PhuThuongStickyRice.CrossCuttingConcerns.Tenants;
using Microsoft.AspNetCore.Http;

namespace PhuThuongStickyRice.WebAPI.Tenants
{
    /// <summary>
    /// Xác định tenant hiện tại từ HTTP request (scheme/host/port).
    /// Tạm thời luôn trả về tenant mặc định — TODO: tra cứu thông tin tenant
    /// từ kho cấu hình thay vì dựng object rỗng.
    /// </summary>
    public class TenantResolver : ITenantResolver
    {
        private readonly IHttpContextAccessor _context;

        /// <summary>
        /// Khởi tạo resolver với accessor đọc HttpContext của request đang xử lý.
        /// </summary>
        /// <param name="context">Accessor cung cấp HttpContext của request hiện tại.</param>
        public TenantResolver(IHttpContextAccessor context)
        {
            _context = context;
        }

        /// <summary>
        /// Tenant của request hiện tại; null khi không có HttpContext
        /// (ví dụ: chạy trong background worker, không qua HTTP).
        /// </summary>
        public Tenant Tenant
        {
            get
            {
                var request = _context?.HttpContext?.Request;

                if (request is null)
                {
                    return null;
                }

                var scheme = request.Scheme;
                var host = request.Host.Host;
                var port = request.Host.Port;

                // TODO: query configuration store to get Tenant information
                // Tạm thời chưa có kho cấu hình tenant nên chưa dùng scheme/host/
                // port để phân biệt — mọi request đều nhận tenant mặc định rỗng,
                // và resolver connection string sẽ fallback về DB chung.
                return new Tenant
                {
                };
            }
        }
    }
}
