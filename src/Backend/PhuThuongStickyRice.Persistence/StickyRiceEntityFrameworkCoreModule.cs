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

            RegisterSqlServer(context, connectionString);
            RegisterRepositories(context.Services);
            RegisterInfrastructurePrimitives(context.Services, connectionString);
        }

        /// <summary>
        /// Cấu hình SQL Server chỉ khi có connection string. Test host (InMemory)
        /// không có connection string và tự cấu hình provider riêng qua
        /// AbpDbContextOptions; cấu hình cả 2 provider sẽ làm EF Core lỗi
        /// multiple providers.
        /// </summary>
        private void RegisterSqlServer(ServiceConfigurationContext context, string connectionString)
        {
            var hasConnectionString = !string.IsNullOrWhiteSpace(connectionString);
            if (!hasConnectionString)
            {
                return;
            }

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

        /// <summary>Đăng ký repository cho các aggregate đặt hàng và template.</summary>
        private static void RegisterRepositories(IServiceCollection services)
        {
            // ABP repositories for the ordering aggregates
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IWalletAccountRepository, WalletAccountRepository>();
            services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
            services.AddScoped<ICheckInRepository, CheckInRepository>();
            services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();

            // Template repositories + template IUnitOfWork for the legacy
            // CommandQueryDispatcher/EntityCrudService paths.
            services.AddScoped(typeof(IUnitOfWork), sp => sp.GetRequiredService<StickyRiceDbContext>());
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            // CA2263: overload generic an toàn kiểu lúc compile (xem chú thích ở
            // PersistenceExtensions.AddRepositories) — dòng open-generic giữ nguyên.
            services.AddScoped<IAuditLogEntryRepository, AuditLogEntryRepository>();
            services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
        }

        /// <summary>Đăng ký lock phân tán và circuit breaker dùng chung hạ tầng.</summary>
        private static void RegisterInfrastructurePrimitives(IServiceCollection services, string connectionString)
        {
            services.AddScoped<IDistributedLockProvider, SqlServerDistributedLockProvider>();
            services.AddScoped<ICircuitBreakerStore, CircuitBreakerStore>();
            services.AddScoped(typeof(IDistributedLock), _ => new SqlDistributedLock(connectionString));
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
