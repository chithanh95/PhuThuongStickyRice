using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Persistence;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Services
{
    /// <summary>
    /// Seeds the <c>Admin</c> role and the default admin account at startup,
    /// only if no admin exists yet. Never crashes startup: if the database
    /// is not migrated yet (the Migrator runs migrations), it logs an error
    /// and retries a few times, then gives up loudly.
    /// Change the default password immediately after the first login.
    /// </summary>
    public class AdminSeedHostedService : IHostedService
    {
        public const string DefaultAdminEmail = "admin@phuthuong.vn";

        public const string DefaultAdminPassword = "Admin@123";

        public const string AdminRoleName = "Admin";

        /// <summary>Item 126: kitchen display staff.</summary>
        public const string KitchenRoleName = "Kitchen";

        /// <summary>Item 126: counter staff (orders / POS / customers, no finance pages).</summary>
        public const string CashierRoleName = "Cashier";

        /// <summary>Registered customer (self-service account).</summary>
        public const string CustomerRoleName = "Customer";

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AdminSeedHostedService> _logger;

        public AdminSeedHostedService(IServiceProvider serviceProvider, ILogger<AdminSeedHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Retry a few times: at startup the database may not be reachable
            // yet (SQL Server still starting, migrations just applied).
            // Failures are logged as ERRORS (never silently swallowed) so a
            // broken seed is visible in the Output window immediately.
            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await SeedOnceAsync(cancellationToken);
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    _logger.LogError(ex,
                        "Admin seed attempt {Attempt}/{MaxAttempts} failed, retrying in 5s.",
                        attempt, maxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Admin seed FAILED after {MaxAttempts} attempts. " +
                        "If the database is not migrated yet, run the Migrator project first. " +
                        "You can also create the admin manually with deploy/seed-admin.sql. " +
                        "Login as '{Email}' will return 401 until the account exists.",
                        maxAttempts, DefaultAdminEmail);
                }
            }
        }

        private async Task SeedOnceAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Admin seed: starting (attempt to ensure '{Email}' exists).", DefaultAdminEmail);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<StickyRiceDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            _logger.LogInformation("Admin seed: database connection OK.");

            var role = await dbContext.Set<Role>()
                .FirstOrDefaultAsync(r => r.NormalizedName == AdminRoleName.ToUpperInvariant(), cancellationToken);
            if (role == null)
            {
                role = new Role
                {
                    Name = AdminRoleName,
                    NormalizedName = AdminRoleName.ToUpperInvariant(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                await dbContext.Set<Role>().AddAsync(role, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Admin seed: role '{Role}' created.", AdminRoleName);
            }

            // Item 126: seed the Kitchen and Cashier roles too (no users
            // assigned — the admin assigns them on the user admin page).
            foreach (var extraRole in new[] { KitchenRoleName, CashierRoleName, CustomerRoleName })
            {
                var exists = await dbContext.Set<Role>()
                    .AnyAsync(r => r.NormalizedName == extraRole.ToUpperInvariant(), cancellationToken);
                if (!exists)
                {
                    await dbContext.Set<Role>().AddAsync(new Role
                    {
                        Name = extraRole,
                        NormalizedName = extraRole.ToUpperInvariant(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    }, cancellationToken);
                    _logger.LogInformation("Admin seed: role '{Role}' created.", extraRole);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var admin = await userManager.FindByEmailAsync(DefaultAdminEmail);
            if (admin == null)
            {
                _logger.LogInformation("Admin seed: creating default admin user '{Email}'.", DefaultAdminEmail);
                admin = new User
                {
                    UserName = DefaultAdminEmail,
                    Email = DefaultAdminEmail,
                    EmailConfirmed = true,
                    LockoutEnabled = true
                };

                var result = await userManager.CreateAsync(admin, DefaultAdminPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    _logger.LogError("Admin seed: could not create the default admin user: {Errors}", errors);
                    throw new InvalidOperationException($"Admin seed: UserManager.CreateAsync failed: {errors}");
                }
                _logger.LogInformation("Admin seed: user '{Email}' created.", DefaultAdminEmail);
            }
            else
            {
                _logger.LogInformation("Admin seed: user '{Email}' already exists.", DefaultAdminEmail);
            }

            var alreadyAdmin = await dbContext.Set<UserRole>()
                .AnyAsync(x => x.UserId == admin.Id && x.RoleId == role.Id, cancellationToken);
            if (!alreadyAdmin)
            {
                await dbContext.Set<UserRole>().AddAsync(
                    new UserRole { UserId = admin.Id, RoleId = role.Id }, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Admin seed: role '{Role}' assigned to '{Email}'.", AdminRoleName, DefaultAdminEmail);
            }

            _logger.LogInformation("Admin seed: DONE. '{Email}' has the '{Role}' role.", DefaultAdminEmail, AdminRoleName);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
