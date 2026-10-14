using Microsoft.JSInterop;
using PhuThuongStickyRice.Blazor.Modules.Shop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Shop.Services
{
    /// <summary>
    /// Client-side shopping cart state, persisted to browser localStorage
    /// so a page refresh does not wipe the cart.
    /// </summary>
    public class CartState
    {
        private const string StorageKey = "phuthuong.cart.v1";

        private readonly IJSRuntime _jsRuntime;
        private readonly List<CartItemModel> _items = new List<CartItemModel>();
        private bool _loaded;

        public CartState(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public event Action OnChange;

        public IReadOnlyList<CartItemModel> Items => _items.AsReadOnly();

        public int TotalQuantity => _items.Sum(x => x.Quantity);

        public decimal TotalAmount => _items.Sum(x => x.LineTotal);

        /// <summary>
        /// Loads persisted items from localStorage. Call once at app startup
        /// (after render, when JS interop is available).
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;

            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var saved = JsonSerializer.Deserialize<List<CartItemModel>>(json);
                    if (saved != null && saved.Count > 0)
                    {
                        _items.AddRange(saved.Where(x => x != null && x.Quantity > 0));
                        NotifyChanged();
                    }
                }
            }
            catch (Exception)
            {
                // Storage unavailable (private mode, blocked, etc.) — cart just
                // stays in-memory for this session.
            }
        }

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

        private void NotifyChanged()
        {
            OnChange?.Invoke();
            _ = PersistAsync();
        }

        private async Task PersistAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_items);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
            }
            catch (Exception)
            {
                // Storage unavailable — ignore, cart still works in-memory.
            }
        }
    }
}
