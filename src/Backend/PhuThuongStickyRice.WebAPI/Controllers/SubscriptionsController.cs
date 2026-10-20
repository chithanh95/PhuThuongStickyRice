using PhuThuongStickyRice.Application.Subscriptions;
using PhuThuongStickyRice.Application.Subscriptions.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
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
    /// API gói đăng ký xôi định kỳ: khách đăng ký public (/dat-dinh-ky),
    /// admin xem danh sách và bật/tắt gói. Gói đang hoạt động sinh đơn
    /// hàng tự động theo lịch hàng ngày.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionAppService _subscriptionAppService;

        /// <summary>
        /// Khởi tạo controller đăng ký xôi định kỳ.
        /// </summary>
        /// <param name="subscriptionAppService">AppService quản lý gói đăng ký xôi hàng ngày (/dat-dinh-ky).</param>
        public SubscriptionsController(ISubscriptionAppService subscriptionAppService)
        {
            _subscriptionAppService = subscriptionAppService;
        }

        /// <summary>
        /// Public: khách đăng ký xôi định kỳ (/dat-dinh-ky); lỗi nghiệp vụ trả 400.
        /// Gói mới vào trạng thái chờ duyệt (chưa sinh đơn) — admin kích hoạt
        /// ở PUT {id}/active thì hệ thống mới tự sinh đơn COD hàng ngày.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateSubscriptionDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _subscriptionAppService.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Admin xem danh sách gói đăng ký. <paramref name="activeOnly"/> = true
        /// thì chỉ lấy các gói đang hoạt động.
        /// </summary>
        /// <param name="activeOnly">Lọc chỉ gói đang hoạt động (null = tất cả).</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] bool? activeOnly,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _subscriptionAppService.GetListAsync(activeOnly));
        }

        /// <summary>
        /// Body request bật/tắt gói đăng ký. Gói bị tắt thì ngừng sinh đơn
        /// tự động hàng ngày, nhưng lịch sử đơn cũ vẫn giữ nguyên.
        /// </summary>
        public sealed class SetActiveRequestModel
        {
            /// <summary><c>true</c> bật gói, <c>false</c> tạm dừng gói.</summary>
            public bool Active { get; set; }
        }

        /// <summary>
        /// Admin bật/tắt một gói đăng ký. Tắt gói chỉ dừng sinh đơn mới —
        /// khách đã đăng ký trước đó không bị xóa dữ liệu.
        /// </summary>
        /// <param name="id">Id gói đăng ký.</param>
        /// <param name="model">Trạng thái mong muốn; body null thì coi như tắt.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/active")]
        public async Task<ActionResult> SetActive(
            Guid id,
            [FromBody] SetActiveRequestModel model,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _subscriptionAppService.SetActiveAsync(id, model?.Active ?? false));
        }
    }
}
