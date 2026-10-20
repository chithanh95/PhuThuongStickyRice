using PhuThuongStickyRice.Application.Inventory;
using PhuThuongStickyRice.Application.Inventory.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// End-of-day inventory counts (kiểm kê cuối ngày, item 87).
    /// Admin only.
    /// </summary>
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/admin/inventory")]
    [ApiController]
    public partial class AdminInventoryController : ControllerBase
    {
        private readonly IInventoryAppService _inventoryAppService;
        private readonly ILogger<AdminInventoryController> _logger;

        public AdminInventoryController(
            IInventoryAppService inventoryAppService,
            ILogger<AdminInventoryController> logger)
        {
            _inventoryAppService = inventoryAppService;
            _logger = logger;
        }

        /// <summary>Saves the closing counts for a date (replaces existing lines).</summary>
        [HttpPost]
        public async Task<ActionResult> Save([FromBody] SaveDailyInventoryInput input,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _inventoryAppService.SaveAsync(input);
                return Ok(new { ok = true });
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                LogSaveDailyInventoryFailed(_logger, ex);
                return StatusCode(500, "Lưu kiểm kê thất bại. Vui lòng thử lại.");
            }
        }

        /// <summary>Inventory history for the last N days (default 30), newest first.</summary>
        [HttpGet("history")]
        public async Task<ActionResult<List<DailyInventoryDayDto>>> GetHistory(
            [FromQuery] int days = 30,
            CancellationToken cancellationToken = default)
        {
            var history = await _inventoryAppService.GetHistoryAsync(days);
            return Ok(history);
        }

        // CA1848: logging hiệu năng cao bằng LoggerMessage source-generated
        // (tránh cấp phát params array + boxing mỗi lần log).

        /// <summary>Log khi lưu kiểm kê hằng ngày thất bại.</summary>
        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save daily inventory.")]
        private static partial void LogSaveDailyInventoryFailed(ILogger logger, Exception ex);
    }
}
