using PhuThuongStickyRice.CrossCuttingConcerns.Tenants;
using Microsoft.EntityFrameworkCore;

namespace PhuThuongStickyRice.Persistence
{
    public class StickyRiceDbContextMultiTenant : StickyRiceDbContext
    {
        private readonly IConnectionStringResolver<StickyRiceDbContextMultiTenant> _connectionStringResolver;

        public StickyRiceDbContextMultiTenant(
            IConnectionStringResolver<StickyRiceDbContextMultiTenant> connectionStringResolver)
            : base(new DbContextOptions<StickyRiceDbContext>())
        {
            _connectionStringResolver = connectionStringResolver;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionStringResolver.ConnectionString);
        }
    }
}
