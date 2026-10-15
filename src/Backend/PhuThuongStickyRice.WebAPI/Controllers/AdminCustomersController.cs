using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Item 168: admin customer endpoints — purchase-history detail by phone.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin,Cashier")]
    [Route("api/admin/customers")]
    [ApiController]
    public class AdminCustomersController : ControllerBase
    {
        private readonly ICustomerDetailAppService _detail;

        public AdminCustomersController(ICustomerDetailAppService detail)
        {
            _detail = detail;
        }

        /// <summary>
        /// Item 168: detailed purchase history of one customer (by phone) —
        /// totals, first/last order, top 3 products, 10 most recent orders,
        /// CRM tags. Cancelled orders are counted separately.
        /// </summary>
        [HttpGet("{phone}/detail")]
        public async Task<ActionResult> GetDetail(string phone)
        {
            return Ok(await _detail.GetDetailAsync(phone));
        }
    }
}
