#nullable enable annotations

using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Common.ImageBackup;
using PhuThuongStickyRice.WebAPI.Common;
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

        // Tên provider mặc định: không backup đi đâu, đĩa local là bản duy nhất.
        private const string NoneProviderName = "None";

        // Worker backup ảnh chạy mỗi giờ 1 lần: file ảnh upload không đổi
        // liên tục nên quét hourly là đủ, không cần realtime.
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

        public ImageBackupWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            Timer.Period = (int)CheckInterval.TotalMilliseconds;
            Timer.RunOnStart = false;
        }

        protected override async Task DoWorkAsync(
            PeriodicBackgroundWorkerContext workerContext)
        {
            var provider = workerContext.ServiceProvider;

            var providerName = await ResolveProviderNameAsync(provider);
            if (providerName == null)
            {
                return;
            }

            var backupProvider = ResolveBackupProvider(provider, providerName);
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

            if (!configured || string.Equals(backupProvider.Name, NoneProviderName, StringComparison.OrdinalIgnoreCase))
            {
                return; // Nothing to do — the default "None" provider is a no-op.
            }

            var env = provider.GetService<IWebHostEnvironment>();
            var root = WebRootResolver.Resolve(env);
            var uploads = Path.Combine(root, "uploads");
            if (!Directory.Exists(uploads))
            {
                return;
            }

            var state = LoadState(uploads);
            var backedUp = await BackupNewFilesAsync(backupProvider, uploads, state);

            if (backedUp > 0)
            {
                SaveState(uploads, state);
                Logger.LogInformation("ImageBackupWorker: backed up {Count} new files via '{Provider}'.", backedUp, backupProvider.Name);
            }
        }

        /// <summary>
        /// Đọc tên provider backup từ cài đặt shop. Trả về null khi không đọc
        /// được cài đặt (caller sẽ dừng worker lần này).
        /// </summary>
        private async Task<string?> ResolveProviderNameAsync(IServiceProvider provider)
        {
            try
            {
                var settings = provider.GetRequiredService<IShopSettingAppService>();
                var dto = await settings.GetAsync();
                return string.IsNullOrWhiteSpace(dto.ImageBackupProvider)
                    ? NoneProviderName
                    : dto.ImageBackupProvider.Trim();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ImageBackupWorker: could not read shop settings.");
                return null;
            }
        }

        /// <summary>
        /// Tìm provider backup theo tên, fallback về provider "None" khi không khớp.
        /// </summary>
        private static IImageBackupProvider? ResolveBackupProvider(
            IServiceProvider provider, string providerName)
        {
            var providers = provider.GetServices<IImageBackupProvider>();
            return providers
                .FirstOrDefault(x => string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase))
                ?? providers.FirstOrDefault(x => string.Equals(x.Name, NoneProviderName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Backup các file chưa từng backup thành công. Trả về số file đã backup.
        /// </summary>
        private async Task<int> BackupNewFilesAsync(
            IImageBackupProvider backupProvider,
            string uploads,
            HashSet<string> state)
        {
            var backedUp = 0;

            // codequality-team 2026-10-20 run-0245 Agent3-BestPractices: EnumerateFiles
            // (lazy) thay GetFiles — chỉ dùng trong foreach nên tương đương,
            // không materialize toàn bộ mảng path khi folder lớn.
            foreach (var file in Directory.EnumerateFiles(uploads, "*", SearchOption.AllDirectories))
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

            return backedUp;
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
