using Microsoft.AspNetCore.Authorization;
using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PhuThuongStickyRice.Application.Shop
{
    public interface ICartAppService : IApplicationService
    {
        /// <summary>Item 137: lấy giỏ hàng của user đang đăng nhập (null nếu chưa có).</summary>
        Task<string> GetAsync();

        /// <summary>Item 137: lưu giỏ hàng của user đang đăng nhập.</summary>
        Task SaveAsync(string itemsJson);
    }

    [Authorize]
    public class CartAppService : ApplicationService, ICartAppService
    {
        private readonly IRepository<ShoppingCart, Guid> _carts;

        public CartAppService(IRepository<ShoppingCart, Guid> carts)
        {
            _carts = carts;
        }

        public virtual async Task<string> GetAsync()
        {
            var userId = CurrentUser?.Id?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var cart = await _carts.FirstOrDefaultAsync(x => x.UserId == userId);
            return cart?.ItemsJson;
        }

        public virtual async Task SaveAsync(string itemsJson)
        {
            var userId = CurrentUser?.Id?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var cart = await _carts.FirstOrDefaultAsync(x => x.UserId == userId);
            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    UserId = userId,
                    ItemsJson = itemsJson,
                    UpdatedAt = DateTime.UtcNow
                };
                await _carts.InsertAsync(cart, autoSave: true);
            }
            else
            {
                cart.ItemsJson = itemsJson;
                cart.UpdatedAt = DateTime.UtcNow;
                await _carts.UpdateAsync(cart, autoSave: true);
            }
        }
    }
}
