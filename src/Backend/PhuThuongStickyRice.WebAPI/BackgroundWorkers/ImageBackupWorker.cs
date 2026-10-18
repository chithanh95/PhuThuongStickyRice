using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Common.ImageBackup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace PhuThuongStickyRice.WebAPI.BackgroundWorkers
{
    /// <summary>
    /// Image backup worker (item 114): hourly, copies new files from
    /// <c>wwwroot/uploads</c> to the configured <see cref="IImageBackupProvider"/>.
    /// The default provider is "None" (does nothing — local disk is the only
    /// copy). Synced files are tracked in <c>uploads/.backup-state.json</c>.
    /// Compile-verified only (no SQL Server in this sandbox).
    /// </summary>
    public class ImageBackupWorker : AsyncPeriodicBackgroundWorkerBase
    {
        private const string StateFileName = ".backup-state.json";

        public ImageBackupWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            Timer.Period = 60 * 60 * 1000; // 1 hour
            Timer.RunOnStart = false;
        }

        protected override async Task DoWorkAsync(
            PeriodicBackgroundWorkerContext workerContext)
        {
            var provider = workerContext.ServiceProvider;

            string providerName;
            try
            {
                var settings = provider.GetRequiredService<IShopSettingAppService>();
                var dto = await settings.GetAsync();
                providerName = string.IsNullOrWhiteSpace(dto.ImageBackupProvider)
                    ? "None"
                    : dto.ImageBackupProvider.Trim();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ImageBackupWorker: could not read shop settings.");
                return;
            }

            var backupProvider = provider
                .GetServices<IImageBackupProvider>()
                .FirstOrDefault(x => string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase))
                ?? provider.GetServices<IImageBackupProvider>()
                    .FirstOrDefault(x => string.Equals(x.Name, "None", StringComparison.OrdinalIgnoreCase));

            if (backupProvider == null)
            {
                return;
            }

            bool configured;
            try
            {
                configured = await backupProvider.IsConfiguredAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ImageBackupWorker: provider check failed.");
                return;
            }

            if (!configured || string.Equals(backupProvider.Name, "None", StringComparison.OrdinalIgnoreCase))
            {
                return; // Nothing to do — the default "None" provider is a no-op.
            }

            var env = provider.GetService<IWebHostEnvironment>();
            var root = env?.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var uploads = Path.Combine(root, "uploads");
            if (!Directory.Exists(uploads))
            {
                return;
            }

            var state = LoadState(uploads);
            var backedUp = 0;

            foreach (var file in Directory.GetFiles(uploads, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(file);
                if (string.Equals(name, StateFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(uploads, file).Replace('\\', '/');
                if (state.Contains(relative))
                {
                    continue;
                }

                try
                {
                    using var stream = File.OpenRead(file);
                    await backupProvider.BackupFileAsync(relative, stream);
                    state.Add(relative);
                    backedUp++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "ImageBackupWorker: failed to back up {File}.", relative);
                }
            }

            if (backedUp > 0)
            {
                SaveState(uploads, state);
                Logger.LogInformation("ImageBackupWorker: backed up {Count} new files via '{Provider}'.", backedUp, backupProvider.Name);
            }
        }

        private static HashSet<string> LoadState(string uploads)
        {
            try
            {
                var path = Path.Combine(uploads, StateFileName);
                if (!File.Exists(path))
                {
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                var list = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(path));
                return new HashSet<string>(list ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void SaveState(string uploads, HashSet<string> state)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(uploads, StateFileName),
                    JsonSerializer.Serialize(state.ToList()));
            }
            catch (Exception)
            {
                // Best effort — the next run simply retries the files.
            }
        }
    }
}
