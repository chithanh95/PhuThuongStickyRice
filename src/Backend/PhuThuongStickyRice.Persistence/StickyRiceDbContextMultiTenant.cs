using PhuThuongStickyRice.CrossCuttingConcerns.Tenants;
using Microsoft.EntityFrameworkCore;

namespace PhuThuongStickyRice.Persistence
{
    /// <summary>
    /// DbContext cho chế độ multi-tenant: connection string không đọc từ
    /// appsettings mà resolve động qua <c>IConnectionStringResolver</c> để mỗi
    /// tenant dùng database riêng. Kế thừa toàn bộ mapping/DbSet từ
    /// <see cref="StickyRiceDbContext"/> nên chỉ override cách lấy chuỗi kết nối.
    /// </summary>
    public class StickyRiceDbContextMultiTenant : StickyRiceDbContext
    {
        private readonly IConnectionStringResolver<StickyRiceDbContextMultiTenant> _connectionStringResolver;

        /// <summary>
        /// Khởi tạo context với bộ resolve connection string của tenant hiện tại.
        /// </summary>
        /// <param name="connectionStringResolver">Resolve chuỗi kết nối theo tenant đang xử lý.</param>
        public StickyRiceDbContextMultiTenant(
            IConnectionStringResolver<StickyRiceDbContextMultiTenant> connectionStringResolver)
            // Truyền options trống của base type cho constructor cha — chuỗi kết nối
            // thật được gắn ở OnConfiguring sau, khi tenant đã được xác định.
            : base(new DbContextOptions<StickyRiceDbContext>())
        {
            _connectionStringResolver = connectionStringResolver;
        }

        /// <summary>
        /// Gắn chuỗi kết nối SQL Server của tenant hiện tại vào options.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionStringResolver.ConnectionString);
        }
    }
}
