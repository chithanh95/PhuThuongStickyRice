using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PhuThuongStickyRice.Persistence;

namespace PhuThuongStickyRice.Migrator.Migrations
{
    /// <summary>
    /// Design-time factory so <c>dotnet ef migrations add</c> can build the
    /// model without running the app or needing a live database. The
    /// connection string is never used during scaffolding.
    /// Usage (from the repo root):
    /// <code>
    /// dotnet ef migrations add &lt;Name&gt; \
    ///   --project src/Backend/PhuThuongStickyRice.Migrator \
    ///   --startup-project src/Backend/PhuThuongStickyRice.Migrator \
    ///   --context StickyRiceDbContext \
    ///   --output-dir Migrations/StickyRiceDb
    /// </code>
    /// </summary>
    public class StickyRiceDbContextFactory : IDesignTimeDbContextFactory<StickyRiceDbContext>
    {
        public StickyRiceDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<StickyRiceDbContext>();
            optionsBuilder.UseSqlServer(
                "Server=(local);Database=PhuThuongStickyRice;Trusted_Connection=True;TrustServerCertificate=True",
                sql => sql.MigrationsAssembly("PhuThuongStickyRice.Migrator"));

            return new StickyRiceDbContext(optionsBuilder.Options);
        }
    }
}
