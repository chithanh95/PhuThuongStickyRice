
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Reports;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>Item 84: profit report — revenue − cost = gross profit (Admin).</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProfitReportController : ControllerBase
    {
        private readonly IProfitReportAppService _profitReport;

        public ProfitReportController(IProfitReportAppService profitReport)
        {
            _profitReport = profitReport;
        }

        /// <summary>
        /// Admin: báo cáo lãi gộp theo ngày và theo món trong khoảng ngày.
        /// Lãi = doanh thu − giá vốn; giá vốn dùng giá HIỆN TẠI của từng món
        /// (tính hồi tố — sửa giá vốn làm đổi số liệu các kỳ trước); đơn đã
        /// hủy bị loại khỏi toàn bộ số liệu.
        /// </summary>
        /// <param name="from">Ngày bắt đầu (mặc định 7 ngày gần nhất).</param>
        /// <param name="to">Ngày kết thúc (mặc định hôm nay).</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        /// <returns>Doanh thu/giá vốn/lãi theo ngày, theo món và tổng cộng.</returns>
        [HttpGet]
        public async Task<ActionResult> GetReport(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _profitReport.GetReportAsync(from, to));
        }
    }
}
