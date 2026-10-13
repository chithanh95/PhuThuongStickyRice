using PhuThuongStickyRice.Blazor.Modules.Shop.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhuThuongStickyRice.Blazor.Modules.Shop.Services
{
    /// <summary>
    /// Client-side shopping cart state.
    /// </summary>
    public class CartState
    {
        private readonly List<CartItemModel> _items = new List<CartItemModel>();

        public event Action OnChange;

        public IReadOnlyList<CartItemModel> Items => _items.AsReadOnly();

        public int TotalQuantity => _items.Sum(x => x.Quantity);

        public decimal TotalAmount => _items.Sum(x => x.LineTotal);

        public void Add(ShopProductModel product, int quantity = 1)
        {
            var existing = _items.FirstOrDefault(x => x.ProductId == product.Id);
            if (existing == null)
            {
                _items.Add(new CartItemModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCode = product.Code,
                    UnitPrice = product.Price,
                    Quantity = quantity
                });
            }
            else
            {
                existing.Quantity += quantity;
            }

            NotifyChanged();
        }

        public void UpdateQuantity(Guid productId, int quantity)
        {
            var existing = _items.FirstOrDefault(x => x.ProductId == productId);
            if (existing == null)
            {
                return;
            }

            if (quantity < 1)
            {
                _items.Remove(existing);
            }
            else
            {
                existing.Quantity = quantity;
            }

            NotifyChanged();
        }

        public void Remove(Guid productId)
        {
            _items.RemoveAll(x => x.ProductId == productId);
            NotifyChanged();
        }

        public void Clear()
        {
            _items.Clear();
            NotifyChanged();
        }

        private void NotifyChanged() => OnChange?.Invoke();
    }
}
