using PhuThuongStickyRice.Application.Zalo;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// API quản trị tích hợp bên thứ ba (Zalo OA...): xem trạng thái cấu hình
    /// và gửi tin nhắn kiểm tra. Toàn bộ endpoint chỉ dành cho Admin.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class IntegrationsController : ControllerBase
    {
        private readonly ZaloOaOptions _zaloOptions;
        private readonly IZaloNotificationService _zalo;

        /// <summary>Khởi tạo controller kiểm tra tích hợp (Zalo OA).</summary>
        public IntegrationsController(
            ZaloOaOptions zaloOptions,
            IZaloNotificationService zalo)
        {
            _zaloOptions = zaloOptions;
            _zalo = zalo;
        }

        /// <summary>Trạng thái cấu hình Zalo OA (token đã được che).</summary>
        [HttpGet("zalo")]
        public ActionResult GetZaloStatus(CancellationToken cancellationToken = default)
        {
            return Ok(new
            {
                configured = _zaloOptions.IsConfigured,
                oaId = Mask(_zaloOptions.OaId),
                accessToken = Mask(_zaloOptions.AccessToken),
                note = "Điền AccessToken và OaId thật vào appsettings (mục ZaloOa) để bật gửi tin nhắn. Xem README mục \"Zalo OA\"."
            });
        }

        /// <summary>
        /// Body request gửi tin nhắn Zalo OA kiểm tra về SĐT của admin.
        /// </summary>
        public sealed class ZaloTestRequestModel
        {
            /// <summary>Số điện thoại Zalo của người nhận tin test.</summary>
            public string Phone { get; set; }

            /// <summary>Nội dung tin test; để trống thì dùng mẫu mặc định.</summary>
            public string Message { get; set; }
        }

        /// <summary>
        /// Gửi tin nhắn test qua service Zalo. Khi chưa cấu hình (giá trị
        /// placeholder "xxx") service chỉ ghi log "would send" thay vì gọi
        /// API thật — để admin kiểm tra mà không gửi tin nhắn nhầm ra ngoài.
        /// </summary>
        [HttpPost("zalo/test")]
        public async Task<ActionResult> TestZalo(
            [FromBody] ZaloTestRequestModel model,
            CancellationToken cancellationToken = default)
        {
            var message = string.IsNullOrWhiteSpace(model?.Message)
                ? "Xôi Phú Thượng: đây là tin nhắn kiểm tra tích hợp Zalo OA."
                : model.Message;
            await _zalo.SendTextMessageAsync(model?.Phone ?? "0900000000", message);
            return Ok(new { sent = _zaloOptions.IsConfigured, stubbed = !_zaloOptions.IsConfigured });
        }

        /// <summary>
        /// Che secret/token khi trả về màn hình trạng thái: giữ 4 ký tự cuối
        /// để admin nhận biết, phần còn lại thay bằng '*' — token thật không bao
        /// giờ rời server qua API này.
        /// </summary>
        /// <param name="value">Giá trị cần che (access token, OA id).</param>
        /// <returns>Chuỗi đã che hoặc "xxx (chưa cấu hình)" khi chưa có giá trị.</returns>
        private static string Mask(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "xxx")
            {
                return "xxx (chưa cấu hình)";
            }

            return value.Length <= 4
                ? "****"
                : string.Concat(new string('*', value.Length - 4), value.AsSpan(value.Length - 4));
        }
    }
}
