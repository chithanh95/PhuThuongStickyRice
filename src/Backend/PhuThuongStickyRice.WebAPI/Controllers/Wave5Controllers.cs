using PhuThuongStickyRice.Application.Wave5;
using PhuThuongStickyRice.Application.Wave5.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    // ================= Check-in (item 121) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CheckInsController : ControllerBase
    {
        private readonly ICheckInAppService _checkIns;

        public CheckInsController(ICheckInAppService checkIns)
        {
            _checkIns = checkIns;
        }

        /// <summary>Public: daily check-in; awards loyalty points once per day.</summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<CheckInResultDto>> CheckIn(
            [FromBody] CheckInRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _checkIns.CheckInAsync(model?.Phone));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Public: today's check-in state + streak for a phone.</summary>
        [AllowAnonymous]
        [HttpGet("status")]
        public async Task<ActionResult<CheckInResultDto>> GetStatus(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _checkIns.GetStatusAsync(phone));
        }
    }

    public class CheckInRequestModel
    {
        public string Phone { get; set; }
    }

    // ================= Corporate clients (item 122) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class CorporateClientsController : ControllerBase
    {
        private readonly ICorporateClientAppService _clients;

        public CorporateClientsController(ICorporateClientAppService clients)
        {
            _clients = clients;
        }

        [HttpGet]
        public async Task<ActionResult<List<CorporateClientDto>>> GetList(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _clients.GetListAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CorporateClientDto>> Get(Guid id,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _clients.GetAsync(id));
        }

        [HttpPost]
        public async Task<ActionResult<CorporateClientDto>> Create(
            [FromBody] CreateUpdateCorporateClientDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _clients.CreateAsync(dto));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CorporateClientDto>> Update(Guid id,
            [FromBody] CreateUpdateCorporateClientDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _clients.UpdateAsync(id, dto));
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id,
            CancellationToken cancellationToken = default)
        {
            await _clients.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>Due today (same weekday) — admin converts to order manually.</summary>
        [HttpGet("due-today")]
        public async Task<ActionResult<List<CorporateClientDto>>> GetDueToday(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _clients.GetDueTodayAsync());
        }
    }

    // ================= Customer photos (item 123) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerPhotosController : ControllerBase
    {
        private const long MaxImageBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedImageExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        private readonly ICustomerPhotoAppService _photos;
        private readonly IWebHostEnvironment _environment;

        public CustomerPhotosController(
            ICustomerPhotoAppService photos,
            IWebHostEnvironment environment)
        {
            _photos = photos;
            _environment = environment;
        }

        /// <summary>Public: approved gallery photos.</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<CustomerPhotoDto>>> GetApproved(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _photos.GetApprovedAsync());
        }

        /// <summary>
        /// Public: shares a photo of a received dish (from track-order).
        /// Goes to moderation first (IsApproved = false).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("upload")]
        [RequestSizeLimit(MaxImageBytes)]
        public async Task<ActionResult<CustomerPhotoDto>> Upload(
            [FromForm] IFormFile file,
            [FromForm] string phone,
            [FromForm] string caption,
            [FromForm] string name,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn một file ảnh." });
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    message = "Định dạng ảnh không hợp lệ. Chỉ nhận: jpg, jpeg, png, webp, gif."
                });
            }

            if (file.Length > MaxImageBytes)
            {
                return BadRequest(new { message = "File ảnh tối đa 5 MB." });
            }

            var folder = Path.Combine(_environment.WebRootPath, "uploads", "customer-photos");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(folder, fileName);
            await using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            try
            {
                var photo = await _photos.RegisterAsync(
                    $"/uploads/customer-photos/{fileName}",
                    new CreateCustomerPhotoDto { Phone = phone, Caption = caption, Name = name });
                return Ok(photo);
            }
            catch (DomainException ex)
            {
                System.IO.File.Delete(filePath);
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<List<CustomerPhotoDto>>> GetList(
            [FromQuery] bool pendingOnly = false,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _photos.GetListAsync(pendingOnly));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/approve")]
        public async Task<ActionResult> Approve(Guid id,
            CancellationToken cancellationToken = default)
        {
            await _photos.ApproveAsync(id);
            return Ok(new { message = "Đã duyệt ảnh." });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Reject(Guid id,
            CancellationToken cancellationToken = default)
        {
            var photo = await _photos.RejectAsync(id);
            // Remove the file too.
            var fileName = Path.GetFileName(photo.ImageUrl ?? string.Empty);
            if (!string.IsNullOrEmpty(fileName))
            {
                var path = Path.Combine(
                    _environment.WebRootPath, "uploads", "customer-photos", fileName);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            return NoContent();
        }
    }

    // ================= Ingredient forecast (item 124) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ForecastController : ControllerBase
    {
        private readonly IIngredientForecastAppService _forecast;

        public ForecastController(IIngredientForecastAppService forecast)
        {
            _forecast = forecast;
        }

        [HttpGet]
        public async Task<ActionResult<IngredientForecastDto>> Get(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _forecast.GetForecastAsync());
        }
    }

    // ================= Work shifts (item 125) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class WorkShiftsController : ControllerBase
    {
        private readonly IWorkShiftAppService _shifts;

        public WorkShiftsController(IWorkShiftAppService shifts)
        {
            _shifts = shifts;
        }

        /// <summary>Week view (Mon-Sun) containing the given date.</summary>
        [HttpGet("week")]
        public async Task<ActionResult<List<WorkShiftDto>>> GetWeek(
            [FromQuery] DateTime? date,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _shifts.GetWeekAsync(date ?? DateTime.UtcNow));
        }

        [HttpPost]
        public async Task<ActionResult<WorkShiftDto>> Create(
            [FromBody] CreateUpdateWorkShiftDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _shifts.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<WorkShiftDto>> Update(Guid id,
            [FromBody] CreateUpdateWorkShiftDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _shifts.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id,
            CancellationToken cancellationToken = default)
        {
            await _shifts.DeleteAsync(id);
            return NoContent();
        }
    }

    // ================= Wallet (item 140) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class WalletsController : ControllerBase
    {
        private readonly IWalletAppService _wallets;

        public WalletsController(IWalletAppService wallets)
        {
            _wallets = wallets;
        }

        /// <summary>Public: balance for a phone (checkout shows the ví option when ≥ total).</summary>
        [AllowAnonymous]
        [HttpGet("balance")]
        public async Task<ActionResult<WalletAccountDto>> GetBalance(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _wallets.GetBalanceAsync(phone));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<List<WalletAccountDto>>> GetList(
            [FromQuery] string search,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _wallets.GetListAsync(search));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("transactions")]
        public async Task<ActionResult<List<WalletTransactionDto>>> GetTransactions(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _wallets.GetTransactionsAsync(phone));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("adjust")]
        public async Task<ActionResult<WalletAccountDto>> Adjust(
            [FromBody] AdjustWalletInput input,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _wallets.AdjustAsync(input));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // ================= Shift close (item 129) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Cashier")]
    public class ShiftClosesController : ControllerBase
    {
        private readonly IShiftCloseAppService _closes;

        public ShiftClosesController(IShiftCloseAppService closes)
        {
            _closes = closes;
        }

        /// <summary>Auto-summed expected COD for a business day (VN date).</summary>
        [HttpGet("expected")]
        public async Task<ActionResult<object>> GetExpected(
            [FromQuery] DateTime? date,
            CancellationToken cancellationToken = default)
        {
            var expected = await _closes.GetExpectedCodAsync(date ?? DateTime.UtcNow);
            return Ok(new { expected });
        }

        [HttpGet("by-date")]
        public async Task<ActionResult<ShiftCloseDto>> GetByDate(
            [FromQuery] DateTime? date,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _closes.GetByDateAsync(date ?? DateTime.UtcNow));
        }

        [HttpGet]
        public async Task<ActionResult<List<ShiftCloseDto>>> GetList(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _closes.GetListAsync());
        }

        [HttpPost("close")]
        public async Task<ActionResult<ShiftCloseDto>> Close(
            [FromBody] CloseShiftInput input,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _closes.CloseAsync(input ?? new CloseShiftInput()));
        }
    }

    // ================= Refunds (item 130) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RefundsController : ControllerBase
    {
        private readonly IRefundAppService _refunds;

        public RefundsController(IRefundAppService refunds)
        {
            _refunds = refunds;
        }

        [HttpGet]
        public async Task<ActionResult> GetReport(CancellationToken cancellationToken = default)
        {
            return Ok(await _refunds.GetReportAsync());
        }

        [HttpPost("orders/{orderId:guid}/request")]
        public async Task<ActionResult> RequestRefund(Guid orderId,
            [FromBody] RequestRefundInput input,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _refunds.RequestRefundAsync(orderId, input ?? new RequestRefundInput());
                return Ok(new { message = "Đã ghi nhận yêu cầu hoàn tiền." });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("orders/{orderId:guid}/confirm")]
        public async Task<ActionResult> ConfirmRefund(Guid orderId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _refunds.ConfirmRefundAsync(orderId);
                return Ok(new { message = "Đã xác nhận hoàn tiền." });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // ================= Message templates (item 131) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class MessageTemplatesController : ControllerBase
    {
        private readonly IMessageTemplateAppService _templates;

        public MessageTemplatesController(IMessageTemplateAppService templates)
        {
            _templates = templates;
        }

        [HttpGet]
        public async Task<ActionResult<List<MessageTemplateDto>>> GetList(
            [FromQuery] string channel,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _templates.GetListAsync(channel));
        }

        [HttpPost]
        public async Task<ActionResult<MessageTemplateDto>> Create(
            [FromBody] CreateUpdateMessageTemplateDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _templates.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<MessageTemplateDto>> Update(Guid id,
            [FromBody] CreateUpdateMessageTemplateDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _templates.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id,
            CancellationToken cancellationToken = default)
        {
            await _templates.DeleteAsync(id);
            return NoContent();
        }
    }

    // ================= Voucher campaigns (item 132) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class VoucherCampaignsController : ControllerBase
    {
        private readonly IVoucherCampaignAppService _campaigns;

        public VoucherCampaignsController(IVoucherCampaignAppService campaigns)
        {
            _campaigns = campaigns;
        }

        [HttpGet("audiences")]
        public async Task<ActionResult<List<VoucherCampaignAudienceDto>>> GetAudiences(
            [FromQuery] string tier,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _campaigns.GetAudiencesAsync(tier));
        }

        /// <summary>
        /// Generates one personal DiscountCode per recipient phone. Bulk
        /// sending is deferred (needs the Zalo OA / SMS channel) — copy the
        /// codes from the result list.
        /// </summary>
        [HttpPost("generate")]
        public async Task<ActionResult<List<GeneratedVoucherDto>>> Generate(
            [FromBody] CreateVoucherCampaignInput input,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _campaigns.GenerateAsync(input ?? new CreateVoucherCampaignInput()));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // ================= POS (item 127) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Cashier")]
    public class PosController : ControllerBase
    {
        private readonly IPosAppService _pos;

        public PosController(IPosAppService pos)
        {
            _pos = pos;
        }

        /// <summary>
        /// Walk-in order: COD + pickup, paid and Completed immediately.
        /// Returns the finished order (print the receipt via /in-hoa-don).
        /// </summary>
        [HttpPost("orders")]
        public async Task<ActionResult> CreateOrder(
            [FromBody] CreatePosOrderInput input,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _pos.CreateOrderAsync(input));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // ================= MoMo reconciliation (item 128) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class MoMoReconciliationController : ControllerBase
    {
        private readonly IMoMoReconciliationAppService _reconciliation;

        public MoMoReconciliationController(IMoMoReconciliationAppService reconciliation)
        {
            _reconciliation = reconciliation;
        }

        /// <summary>MoMo-paid orders; filter by matched=true/false (omit for all).</summary>
        [HttpGet("orders")]
        public async Task<ActionResult<List<MoMoOrderDto>>> GetOrders(
            [FromQuery] bool? matched,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _reconciliation.GetMoMoOrdersAsync(matched));
        }

        /// <summary>Manual "đánh dấu đã đối soát".</summary>
        [HttpPost("orders/{orderId:guid}/mark-matched")]
        public async Task<ActionResult> MarkMatched(Guid orderId,
            CancellationToken cancellationToken = default)
        {
            await _reconciliation.MarkMatchedAsync(orderId);
            return Ok(new { message = "Đã đánh dấu đối soát." });
        }
    }
}
