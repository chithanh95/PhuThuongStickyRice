using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Services
{
    /// <summary>
    /// Seeds the <c>Admin</c> role and the default admin account at startup,
    /// only if no admin exists yet. Never crashes startup: if the database
    /// is not migrated yet (the Migrator runs migrations), it logs a warning.
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

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AdminSeedHostedService> _logger;

        public AdminSeedHostedService(IServiceProvider serviceProvider, ILogger<AdminSeedHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<StickyRiceDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

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
                }

                // Item 126: seed the Kitchen and Cashier roles too (no users
                // assigned — the admin assigns them on the user admin page).
                foreach (var extraRole in new[] { KitchenRoleName, CashierRoleName })
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
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var admin = await userManager.FindByEmailAsync(DefaultAdminEmail);
                if (admin == null)
                {
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
                        _logger.LogWarning(
                            "Admin seed: could not create the default admin user: {Errors}",
                            string.Join("; ", result.Errors));
                        return;
                    }
                }

                var alreadyAdmin = await dbContext.Set<UserRole>()
                    .AnyAsync(x => x.UserId == admin.Id && x.RoleId == role.Id, cancellationToken);
                if (!alreadyAdmin)
                {
                    await dbContext.Set<UserRole>().AddAsync(
                        new UserRole { UserId = admin.Id, RoleId = role.Id }, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation("Admin seed: '{Email}' has the '{Role}' role.", DefaultAdminEmail, AdminRoleName);
            }
            catch (Exception ex)
            {
                // Database may not exist / not be migrated yet — Migrator owns
                // migrations. Log and continue; the seed runs on every start.
                _logger.LogWarning(ex, "Admin seed skipped (database may not be migrated yet).");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
