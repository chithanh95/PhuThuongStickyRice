using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Wave4.DTOs;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// System endpoints: public maintenance-mode status (item 111) and the
    /// admin disk-usage view (item 113).
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly IShopSettingAppService _shopSettings;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public SystemController(
            IShopSettingAppService shopSettings,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _shopSettings = shopSettings;
            _env = env;
            _configuration = configuration;
        }

        /// <summary>
        /// Public: maintenance-mode flag. The WASM app checks this on
        /// startup and shows a friendly page when it is on (item 111).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("status")]
        public async Task<ActionResult<SystemStatusDto>> GetStatus(
            CancellationToken cancellationToken = default)
        {
            var settings = await _shopSettings.GetAsync();
            return Ok(new SystemStatusDto
            {
                MaintenanceMode = settings.MaintenanceMode,
                MaintenanceMessage = settings.MaintenanceMessage
            });
        }

        /// <summary>
        /// Admin: uploads folder size + file count, DB size estimate (best
        /// effort — may be null when the query is unavailable), and the
        /// over-threshold flag (item 113).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("disk-usage")]
        public async Task<ActionResult<DiskUsageDto>> GetDiskUsage(
            CancellationToken cancellationToken = default)
        {
            var settings = await _shopSettings.GetAsync();

            var root = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var uploads = Path.Combine(root, "uploads");

            long bytes = 0;
            var files = 0;
            try
            {
                if (Directory.Exists(uploads))
                {
                    foreach (var file in Directory.GetFiles(uploads, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            bytes += new FileInfo(file).Length;
                            files++;
                        }
                        catch
                        {
                            // Skip files that disappear mid-scan.
                        }
                    }
                }
            }
            catch
            {
                // Best effort.
            }

            var warningMb = settings.UploadsSizeWarningMb;

            return Ok(new DiskUsageDto
            {
                UploadsBytes = bytes,
                UploadsFileCount = files,
                DatabaseBytes = GetDatabaseSizeBytes(),
                WarningThresholdMb = warningMb,
                OverThreshold = warningMb > 0 && bytes > (long)warningMb * 1024 * 1024
            });
        }

        /// <summary>
        /// SQL Server data-file size (reserved MB from sys.master_files).
        /// Null when unavailable — never throws.
        /// </summary>
        private long? GetDatabaseSizeBytes()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("Default")
                    ?? _configuration.GetConnectionString("StickyRiceDb");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return null;
                }

                using var connection = new SqlConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT SUM(size) * 8.0 / 1024
                    FROM sys.master_files
                    WHERE database_id = DB_ID()";
                var result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return null;
                }

                return (long)(Convert.ToDouble(result) * 1024 * 1024);
            }
            catch
            {
                return null;
            }
        }
    }
}
