using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhuThuongStickyRice.Application.Shop;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Item 137: server-side cart sync for logged-in users.
    /// </summary>
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : AbpController
    {
        private readonly ICartAppService _cartAppService;

        public CartController(ICartAppService cartAppService)
        {
            _cartAppService = cartAppService;
        }

        [HttpGet]
        public async Task<ActionResult<string>> Get()
        {
            var itemsJson = await _cartAppService.GetAsync();
            return Ok(new { ItemsJson = itemsJson });
        }

        [HttpPut]
        public async Task<ActionResult> Save([FromBody] SaveCartRequestModel model)
        {
            await _cartAppService.SaveAsync(model?.ItemsJson);
            return Ok();
        }
    }

    public sealed class SaveCartRequestModel
    {
        public string ItemsJson { get; set; }
    }
}
