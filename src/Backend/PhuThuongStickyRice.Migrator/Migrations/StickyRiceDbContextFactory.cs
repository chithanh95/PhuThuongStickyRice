using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PhuThuongStickyRice.Persistence;

namespace PhuThuongStickyRice.Migrator.Migrations
{
    /// <summary>
    /// Design-time factory so EF Core tools (Update-Database, migrations add)
    /// can build the DbContext without running the app. The connection string
    /// is read from the Migrator's appsettings.json, so editing that file is
    /// enough for both PMC and dotnet run scenarios.
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
            // At design time the assembly lives in bin/<Config>/net10.0,
            // where appsettings.json is copied by the build.
            var basePath = Path.GetDirectoryName(typeof(StickyRiceDbContextFactory).Assembly.Location)
                ?? Directory.GetCurrentDirectory();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var connectionString = configuration.GetConnectionString("PhuThuongStickyRice");

            var optionsBuilder = new DbContextOptionsBuilder<StickyRiceDbContext>();
            optionsBuilder.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly("PhuThuongStickyRice.Migrator"));

            return new StickyRiceDbContext(optionsBuilder.Options);
        }
    }
}
