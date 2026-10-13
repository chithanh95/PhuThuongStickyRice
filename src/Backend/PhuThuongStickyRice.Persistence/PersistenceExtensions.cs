using PhuThuongStickyRice.CrossCuttingConcerns.CircuitBreakers;
using PhuThuongStickyRice.CrossCuttingConcerns.Locks;
using PhuThuongStickyRice.CrossCuttingConcerns.Tenants;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.Persistence.CircuitBreakers;
using PhuThuongStickyRice.Persistence.Locks;
using PhuThuongStickyRice.Persistence.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PersistenceExtensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString, string migrationsAssembly = "")
        {
            services.AddDbContext<StickyRiceDbContext>(options => options.UseSqlServer(connectionString, sql =>
                    {
                        if (!string.IsNullOrEmpty(migrationsAssembly))
                        {
                            sql.MigrationsAssembly(migrationsAssembly);
                        }
                    }))
                    .AddDbContextFactory<StickyRiceDbContext>((Action<DbContextOptionsBuilder>)null, ServiceLifetime.Scoped)
                    .AddRepositories();

            services.AddScoped(typeof(IDistributedLock), _ => new SqlDistributedLock(connectionString));

            return services;
        }

        public static IServiceCollection AddMultiTenantPersistence(this IServiceCollection services, Type connectionStringResolverType, Type tenantResolverType)
        {
            services.AddScoped(typeof(IConnectionStringResolver<StickyRiceDbContextMultiTenant>), connectionStringResolverType);
            services.AddScoped(typeof(ITenantResolver), tenantResolverType);

            services.AddDbContext<StickyRiceDbContextMultiTenant>(options => { })
                    .AddScoped(typeof(StickyRiceDbContext), services =>
                    {
                        return services.GetRequiredService<StickyRiceDbContextMultiTenant>();
                    })
                    .AddRepositories();

            services.AddScoped(typeof(IDistributedLock), services =>
            {
                return new SqlDistributedLock(services.GetRequiredService<IConnectionStringResolver<StickyRiceDbContextMultiTenant>>().ConnectionString);
            });

            return services;
        }

        private static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>))
                    .AddScoped(typeof(IAuditLogEntryRepository), typeof(AuditLogEntryRepository))
                    .AddScoped(typeof(IEmailMessageRepository), typeof(EmailMessageRepository))
                    .AddScoped(typeof(ISmsMessageRepository), typeof(SmsMessageRepository))
                    .AddScoped(typeof(IUserRepository), typeof(UserRepository))
                    .AddScoped(typeof(IRoleRepository), typeof(RoleRepository));

            services.AddScoped(typeof(IUnitOfWork), services =>
            {
                return services.GetRequiredService<StickyRiceDbContext>();
            });

            services.AddScoped<ILockManager, LockManager>();
            services.AddScoped<ICircuitBreakerManager, CircuitBreakerManager>();

            return services;
        }

        public static void MigrateStickyRiceDb(this IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<StickyRiceDbContext>().Database.Migrate();
            }
        }
    }
}
