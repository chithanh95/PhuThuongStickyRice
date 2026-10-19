using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PhuThuongStickyRice.Application.Reports
{
    public interface IWeeklyReportAppService : IApplicationService
    {
        /// <summary>Admin: aggregated numbers for the last full week.</summary>
        Task<WeeklyReportDto> GetWeeklyReportAsync();

        /// <summary>
        /// Admin: builds the report and delivers it via Zalo OA when
        /// configured; otherwise returns the text for manual copy.
        /// </summary>
        Task<WeeklyReportSendResultDto> SendWeeklyReportAsync();
    }

    /// <summary>
    /// Item 138: HTTP surface for the weekly owner report. The heavy
    /// lifting lives in <see cref="IWeeklyReportSender"/> (no auth
    /// attributes) so the BackgroundServer Monday worker can reuse it.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class WeeklyReportAppService : ApplicationService, IWeeklyReportAppService
    {
        private readonly IWeeklyReportSender _sender;

        /// <summary>
        /// Khởi tạo HTTP surface (Admin-only) cho báo cáo tuần; logic nặng nằm
        /// ở <see cref="IWeeklyReportSender"/> để worker BackgroundServer dùng chung.
        /// </summary>
        public WeeklyReportAppService(IWeeklyReportSender sender)
        {
            _sender = sender;
        }

        public virtual Task<WeeklyReportDto> GetWeeklyReportAsync() =>
            _sender.BuildReportAsync();

        public virtual Task<WeeklyReportSendResultDto> SendWeeklyReportAsync() =>
            _sender.SendAsync();
    }
}
