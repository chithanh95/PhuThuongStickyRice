using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhuThuongStickyRice.CrossCuttingConcerns.CircuitBreakers;
using PhuThuongStickyRice.CrossCuttingConcerns.Locks;
using PhuThuongStickyRice.Domain;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Persistence.CircuitBreakers;
using PhuThuongStickyRice.Persistence.Locks;
using PhuThuongStickyRice.Persistence.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.Modularity;

namespace PhuThuongStickyRice.Persistence
{
    [DependsOn(
        typeof(StickyRiceDomainModule),
        typeof(AbpEntityFrameworkCoreSqlServerModule))]
    public class StickyRiceEntityFrameworkCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var connectionString = configuration.GetConnectionString("PhuThuongStickyRice");

            context.Services.AddAbpDbContext<StickyRiceDbContext>(options =>
            {
                options.AddDefaultRepositories(includeAllEntities: true);
            });

            // SQL Server is only configured when a connection string exists.
            // Test hosts (InMemory) provide no connection string and configure
            // their own provider via AbpDbContextOptions instead; configuring
            // both providers would make EF Core fail with multiple providers.
            var hasConnectionString = !string.IsNullOrWhiteSpace(connectionString);
            if (hasConnectionString)
            {
                // IDbContextFactory is required by DataProtection's PersistKeysToDbContext
                // (ABP's AddAbpDbContext does not register one).
                context.Services.AddSingleton<IDbContextFactory<StickyRiceDbContext>>(sp =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<StickyRiceDbContext>();
                    optionsBuilder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("PhuThuongStickyRice.Migrator"));
                    return new StickyRiceDbContextFactory(optionsBuilder.Options);
                });

                Configure<AbpDbContextOptions>(options =>
                {
                    options.Configure(ctx =>
                    {
                        ctx.DbContextOptions.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("PhuThuongStickyRice.Migrator"));
                    });
                });
            }

            // ABP repositories for the ordering aggregates
            context.Services.AddScoped<IProductRepository, ProductRepository>();
            context.Services.AddScoped<IOrderRepository, OrderRepository>();
            context.Services.AddScoped<IWalletAccountRepository, WalletAccountRepository>();
            context.Services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
            context.Services.AddScoped<ICheckInRepository, CheckInRepository>();
            context.Services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();

            // Template repositories + template IUnitOfWork for the legacy
            // CommandQueryDispatcher/EntityCrudService paths.
            context.Services.AddScoped(typeof(IUnitOfWork), sp => sp.GetRequiredService<StickyRiceDbContext>());
            context.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            // CA2263: overload generic an toàn kiểu lúc compile (xem chú thích ở
            // PersistenceExtensions.AddRepositories) — dòng open-generic giữ nguyên.
            context.Services.AddScoped<IAuditLogEntryRepository, AuditLogEntryRepository>();
            context.Services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
            context.Services.AddScoped<IUserRepository, UserRepository>();
            context.Services.AddScoped<IRoleRepository, RoleRepository>();

            context.Services.AddScoped<IDistributedLockProvider, SqlServerDistributedLockProvider>();
            context.Services.AddScoped<ICircuitBreakerStore, CircuitBreakerStore>();
            context.Services.AddScoped(typeof(IDistributedLock), _ => new SqlDistributedLock(connectionString));
        }

        private sealed class StickyRiceDbContextFactory : IDbContextFactory<StickyRiceDbContext>
        {
            private readonly DbContextOptions<StickyRiceDbContext> _options;

            public StickyRiceDbContextFactory(DbContextOptions<StickyRiceDbContext> options)
            {
                _options = options;
            }

            public StickyRiceDbContext CreateDbContext()
            {
                return new StickyRiceDbContext(_options);
            }
        }
    }
}
