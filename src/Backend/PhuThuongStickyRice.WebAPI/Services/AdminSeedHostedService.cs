using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Persistence;
using PhuThuongStickyRice.Application.Permissions;
using System;
using System.Collections.Generic;
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

            var role = await EnsureRoleExistsAsync(dbContext, AdminRoleName, cancellationToken);

            // Item 126: seed the Kitchen and Cashier roles too (no users
            // assigned — the admin assigns them on the user admin page).
            // FIX N+1: gộp 3 lần AnyAsync thành 1 query IN duy nhất.
            var extraRoles = new[] { KitchenRoleName, CashierRoleName, CustomerRoleName };
            // Tính trước ở client để EF dịch Contains thành mệnh đề IN trong SQL.
            var wantedNormalized = extraRoles.Select(n => n.ToUpperInvariant()).ToList();
            var existingRoleNames = await dbContext.Set<Role>()
                .Where(r => wantedNormalized.Contains(r.NormalizedName))
                .Select(r => r.NormalizedName)
                .ToListAsync(cancellationToken);
            foreach (var extraRole in extraRoles)
            {
                if (!existingRoleNames.Contains(extraRole.ToUpperInvariant()))
                {
                    await EnsureRoleExistsAsync(dbContext, extraRole, cancellationToken);
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

            // Item 155: seed default fine-grained permissions (idempotent —
            // only missing role/key pairs are added; admin edits at
            // /admin/phan-quyen are never overwritten).
            await SeedDefaultPermissionsAsync(dbContext, cancellationToken);
        }

        /// <summary>
        /// Tạo role nếu chưa tồn tại (tìm theo NormalizedName), trả về role.
        /// Idempotent nên gọi lại nhiều lần hoặc song song từ 2 khối seed đều an toàn.
        /// </summary>
        private async Task<Role> EnsureRoleExistsAsync(
            StickyRiceDbContext dbContext, string roleName, CancellationToken cancellationToken)
        {
            var normalizedName = roleName.ToUpperInvariant();
            var role = await dbContext.Set<Role>()
                .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);
            if (role == null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = normalizedName,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                await dbContext.Set<Role>().AddAsync(role, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Admin seed: role '{Role}' created.", roleName);
            }

            return role;
        }

        /// <summary>
        /// Seed quyền mặc định cho các role (idempotent — chỉ thêm cặp còn thiếu,
        /// không bao giờ ghi đè quyền admin đã sửa ở /admin/phan-quyen).
        /// </summary>
        /// <param name="dbContext">DbContext đang dùng.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        private async Task SeedDefaultPermissionsAsync(
            StickyRiceDbContext dbContext, CancellationToken cancellationToken)
        {
            var set = dbContext.Set<RolePermission>();
            var added = 0;

            // FIX N+1: bản cũ gọi AnyAsync từng cặp (role, key) trong vòng lặp lồng nhau
            // → hàng chục query lẻ mỗi lần khởi động. Tải toàn bộ cặp đã có về 1 lần
            // vào HashSet rồi chỉ Add những cặp còn thiếu — vẫn idempotent, chỉ còn 1 SELECT.
            var existingKeys = new HashSet<(string Role, string Key)>(
                (await set
                    .Select(x => new { x.Role, x.PermissionKey })
                    .ToListAsync(cancellationToken))
                .Select(x => (x.Role, x.PermissionKey)));

            foreach (var pair in PermissionKeys.DefaultGrants)
            {
                foreach (var key in pair.Value)
                {
                    // HashSet.Add trả về false nếu cặp đã tồn tại → chỉ Add entity khi thật sự thiếu.
                    if (existingKeys.Add((pair.Key, key)))
                    {
                        await set.AddAsync(new RolePermission
                        {
                            Id = Guid.NewGuid(),
                            Role = pair.Key,
                            PermissionKey = key,
                            CreatedAt = DateTime.UtcNow,
                        }, cancellationToken);
                        added++;
                    }
                }
            }

            if (added > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Admin seed: {Count} default role permissions added.", added);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
