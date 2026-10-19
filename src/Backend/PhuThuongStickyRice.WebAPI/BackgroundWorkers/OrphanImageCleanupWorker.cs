using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Threading;

namespace PhuThuongStickyRice.WebAPI.BackgroundWorkers
{
    /// <summary>
    /// Weekly orphan-image cleanup (item 112): scans
    /// <c>wwwroot/uploads/landing</c> and deletes files that are NOT
    /// referenced anywhere in the database (LandingPageContent image URLs,
    /// Article.ImageUrl) AND are older than 7 days. Conservative by design:
    /// when in doubt, the file is kept.
    /// Compile-verified only (no SQL Server in this sandbox).
    /// </summary>
    public class OrphanImageCleanupWorker : AsyncPeriodicBackgroundWorkerBase
    {
        private static readonly TimeSpan OrphanAge = TimeSpan.FromDays(7);

        public OrphanImageCleanupWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            Timer.Period = (int)TimeSpan.FromDays(7).TotalMilliseconds;
            Timer.RunOnStart = false;
        }

        protected override async Task DoWorkAsync(
            PeriodicBackgroundWorkerContext workerContext)
        {
            var provider = workerContext.ServiceProvider;

            var env = provider.GetService<IWebHostEnvironment>();
            var root = env?.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var folder = Path.Combine(root, "uploads", "landing");
            if (!Directory.Exists(folder))
            {
                return;
            }

            HashSet<string> referenced;
            try
            {
                referenced = await CollectReferencedFileNamesAsync(provider);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OrphanImageCleanupWorker: could not read referenced images — aborting (conservative).");
                return;
            }

            var cutoff = DateTime.UtcNow - OrphanAge;
            var deleted = 0;
            foreach (var file in Directory.GetFiles(folder))
            {
                var name = Path.GetFileName(file);
                if (referenced.Contains(name))
                {
                    continue;
                }

                // Conservative: only delete files clearly older than 7 days.
                DateTime createdUtc;
                try
                {
                    createdUtc = File.GetCreationTimeUtc(file);
                }
                // Đọc thời gian tạo file có thể thất bại (file bị khóa, hết quyền
                // truy cập...) — vẫn phải bỏ qua file và tiếp tục như cũ, nhưng
                // chỉ nuốt đúng 2 loại exception có thể xảy ra khi đọc metadata,
                // không nuốt mọi thứ (kể cả lỗi nghiêm trọng như hết bộ nhớ).
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Logger.LogDebug(ex, "OrphanImageCleanupWorker: bỏ qua file {File} (không đọc được thời gian tạo).", name);
                    continue;
                }

                if (createdUtc > cutoff)
                {
                    continue;
                }

                try
                {
                    File.Delete(file);
                    deleted++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "OrphanImageCleanupWorker: could not delete {File}.", name);
                }
            }

            if (deleted > 0)
            {
                Logger.LogInformation("OrphanImageCleanupWorker: deleted {Count} orphan images.", deleted);
            }
        }

        private static async Task<HashSet<string>> CollectReferencedFileNamesAsync(IServiceProvider provider)
        {
            var executer = provider.GetRequiredService<IAsyncQueryableExecuter>();
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var landingRepo = provider.GetRequiredService<IRepository<LandingPageContent, Guid>>();
            var landingQueryable = await landingRepo.GetQueryableAsync();
            var contents = await executer.ToListAsync(landingQueryable);
            foreach (var c in contents)
            {
                AddUrl(referenced, c.HeroImageUrl);
                AddUrl(referenced, c.BannerImageUrl);
                AddUrl(referenced, c.MenuImage1Url);
                AddUrl(referenced, c.MenuImage2Url);
                AddUrl(referenced, c.MenuImage3Url);
                AddUrl(referenced, c.FooterBackgroundUrl);
                AddUrl(referenced, c.PopupImageUrl);
            }

            var articleRepo = provider.GetRequiredService<IRepository<Article, Guid>>();
            var articleQueryable = await articleRepo.GetQueryableAsync();
            var articles = await executer.ToListAsync(articleQueryable);
            foreach (var a in articles)
            {
                AddUrl(referenced, a.ImageUrl);
            }

            return referenced;
        }

        private static void AddUrl(HashSet<string> referenced, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            // Only local uploads under /uploads/landing/ are candidates.
            var path = url.Trim();
            const string prefix = "/uploads/landing/";
            var idx = path.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                return;
            }

            var fileName = path.Substring(idx + prefix.Length).Split('?')[0];
            if (!string.IsNullOrWhiteSpace(fileName) && !fileName.Contains('/'))
            {
                referenced.Add(fileName);
            }
        }
    }
}
