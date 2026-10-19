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
                    .AddScoped(typeof(IUserRepository), typeof(UserRepository))
                    .AddScoped(typeof(IRoleRepository), typeof(RoleRepository));

            services.AddScoped(typeof(IUnitOfWork), services =>
            {
                return services.GetRequiredService<StickyRiceDbContext>();
            });

            services.AddScoped<IDistributedLockProvider, SqlServerDistributedLockProvider>();
            services.AddScoped<ICircuitBreakerStore, CircuitBreakerStore>();

            return services;
        }

        public static void MigrateStickyRiceDb(this IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<StickyRiceDbContext>();

                // One-time (2026-10-18): wave migrations were renamed to feature names.
                // Rename already-applied entries in the history table so EF does not
                // try to re-apply them. Idempotent — safe to run every startup.
                context.Database.ExecuteSqlRaw(@"
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014111156_AddCatalogCombosAndVoting' WHERE [MigrationId] = '20261014111156_AddWave1CatalogFeatures';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014112605_AddLoyaltyFaqAndGift' WHERE [MigrationId] = '20261014112605_Wave2LoyaltyCommerce';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014113411_SeedFaqItems' WHERE [MigrationId] = '20261014113411_SeedWave2Faq';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014114805_AddContentSecurityAndRewards' WHERE [MigrationId] = '20261014114805_Wave3KitchenCommerceSecurity';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014121119_AddBranchesLoggingAndFeatureFlags' WHERE [MigrationId] = '20261014121119_Wave4Features';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014123316_AddCorporateWalletAndShifts' WHERE [MigrationId] = '20261014123316_Wave5Features';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014130248_AddDeliverySlots' WHERE [MigrationId] = '20261014130248_Wave6aOrderingExtras';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014131446_AddOrderReviewsAndPolicies' WHERE [MigrationId] = '20261014131446_Wave6bDishRatingsPolicies';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014132450_AddOrderStatusHistory' WHERE [MigrationId] = '20261014132450_Wave6cOrderOps';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014133130_AddAntiSpamLimits' WHERE [MigrationId] = '20261014133130_Wave6dAntiSpam';");

                context.Database.Migrate();
            }
        }
    }
}
