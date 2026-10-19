using Microsoft.AspNetCore.Authorization;
using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PhuThuongStickyRice.Application.Shop
{
    /// <summary>
    /// Hợp đồng giỏ hàng server (Item 137): giỏ của user đăng nhập được lưu
    /// vào bảng ShoppingCart thay vì localStorage để khách chuyển thiết bị vẫn
    /// giữ nguyên giỏ. Mỗi user chỉ có đúng 1 bản ghi giỏ (upsert theo UserId).
    /// </summary>
    public interface ICartAppService : IApplicationService
    {
        /// <summary>Item 137: lấy giỏ hàng của user đang đăng nhập (null nếu chưa có).</summary>
        Task<string?> GetAsync();

        /// <summary>Item 137: lưu giỏ hàng của user đang đăng nhập.</summary>
        Task SaveAsync(string itemsJson);
    }

    /// <summary>
    /// Item 137: lưu/truy xuất giỏ hàng của user đang đăng nhập.
    /// Giỏ lưu server (bảng ShoppingCart) thay vì localStorage để khách chuyển
    /// thiết bị vẫn giữ nguyên giỏ; mỗi user chỉ có đúng 1 bản ghi giỏ.
    /// </summary>
    [Authorize]
    public class CartAppService : ApplicationService, ICartAppService
    {
        /// <summary>Repository giỏ hàng (bảng ShoppingCart — mỗi user đúng 1 bản ghi).</summary>
        private readonly IRepository<ShoppingCart, Guid> _carts;

        /// <summary>Khởi tạo service giỏ hàng với repository giỏ hàng server.</summary>
        /// <param name="carts">Repository bảng ShoppingCart.</param>
        public CartAppService(IRepository<ShoppingCart, Guid> carts)
        {
            _carts = carts;
        }

        /// <summary>
        /// Lấy giỏ hàng của user đang đăng nhập.
        /// </summary>
        /// <returns>Chuỗi JSON các món trong giỏ; null nếu chưa có hoặc chưa đăng nhập.</returns>
        public virtual async Task<string?> GetAsync()
        {
            var userId = CurrentUser?.Id?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var cart = await _carts.FirstOrDefaultAsync(x => x.UserId == userId);
            return cart?.ItemsJson;
        }

        /// <summary>
        /// Lưu (ghi đè) giỏ hàng của user đang đăng nhập.
        /// Chưa có bản ghi thì tạo mới, đã có thì cập nhật — upsert theo UserId
        /// để mỗi user luôn chỉ có 1 giỏ duy nhất.
        /// </summary>
        /// <param name="itemsJson">Chuỗi JSON các món trong giỏ từ client.</param>
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
