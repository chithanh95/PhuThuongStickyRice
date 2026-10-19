using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PhuThuongStickyRice.Application.Reports
{
    public interface IWeeklyReportAppService : IApplicationService
    {
        /// <summary>
        /// Admin: số liệu tổng hợp của tuần đầy đủ gần nhất (Thứ Hai–Chủ Nhật).
        /// </summary>
        Task<WeeklyReportDto> GetWeeklyReportAsync();

        /// <summary>
        /// Admin: dựng báo cáo và gửi qua Zalo OA nếu đã cấu hình token; nếu
        /// chưa thì trả về nội dung text để copy thủ công.
        /// </summary>
        Task<WeeklyReportSendResultDto> SendWeeklyReportAsync();
    }

    /// <summary>
    /// Item 138: mặt HTTP (Admin-only) của báo cáo tuần. Logic nặng nằm ở
    /// <see cref="IWeeklyReportSender"/> (không gắn auth) để worker
    /// BackgroundServer thứ Hai có thể gọi chung.
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

        /// <summary>
        /// Admin: số liệu tổng hợp của tuần đầy đủ gần nhất (Thứ Hai–Chủ Nhật).
        /// </summary>
        /// <returns>DTO số liệu tuần.</returns>
        public virtual Task<WeeklyReportDto> GetWeeklyReportAsync() =>
            _sender.BuildReportAsync();

        /// <summary>
        /// Admin: dựng báo cáo và gửi qua Zalo OA nếu đã cấu hình token; nếu
        /// chưa thì trả về nội dung text để copy thủ công.
        /// </summary>
        /// <returns>Kết quả gửi (đã gửi hay chưa + nội dung).</returns>
        public virtual Task<WeeklyReportSendResultDto> SendWeeklyReportAsync() =>
            _sender.SendAsync();
    }
}
