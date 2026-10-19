using Microsoft.JSInterop;
using PhuThuongStickyRice.Web.Components.Admin.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Client-side shopping cart state, persisted to browser localStorage
    /// so a page refresh does not wipe the cart. Lines are keyed by
    /// product/combo + size + toppings so different configurations stay
    /// separate.
    /// </summary>
    public class CartState
    {
        private const string StorageKey = "phuthuong.cart.v1";

        private readonly IJSRuntime _jsRuntime;
        private readonly AdminCommerceService _commerceService;
        private readonly List<CartItemModel> _items = new List<CartItemModel>();
        private bool _loaded;

        public CartState(IJSRuntime jsRuntime, AdminCommerceService commerceService)
        {
            _jsRuntime = jsRuntime;
            _commerceService = commerceService;
        }

        public event Action OnChange;

        public IReadOnlyList<CartItemModel> Items => _items.AsReadOnly();

        public int TotalQuantity => _items.Sum(x => x.Quantity);

        public decimal TotalAmount => _items.Sum(x => x.LineTotal);

        /// <summary>
        /// Order source (item 133): set from the shop entry query params
        /// (?ban= → "QR bàn", ?share= → "Link chia sẻ", ?nhom= → "Nhóm",
        /// else "Web"). Sent with the checkout request.
        /// </summary>
        public string Source { get; set; } = "Web";

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
                        foreach (var item in saved.Where(x => x != null && x.Quantity > 0))
                        {
                            // Backwards compatibility with carts saved before
                            // line keys existed.
                            if (string.IsNullOrEmpty(item.LineKey))
                            {
                                item.LineKey = BuildLineKey(
                                    item.ProductId, item.SizeOptionId,
                                    item.ToppingIds, item.IsCombo);
                            }

                            item.ToppingIds ??= new List<Guid>();
                            _items.Add(item);
                        }

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

        /// <summary>
        /// Adds a plain product line (base price, no options).
        /// </summary>
        public void Add(ShopProductModel product, int quantity = 1)
        {
            // Bug fix: quick-add used to leave ImageUrl null, so the cart
            // page showed the "no image" placeholder even when the product
            // has an image in the DB (same ResolveImageUrl pattern as the
            // shop quick-view path).
            var imageUrl = _commerceService.ResolveImageUrl(FirstImageUrl(product?.ImageUrlsJson));
            AddConfigured(
                product.Id, product.Name, product.Code, product.Price, quantity,
                sizeOptionId: null, sizeName: null,
                toppingIds: null, toppingNames: null, toppingsTotal: 0,
                isCombo: false, imageUrl: imageUrl);
        }

        /// <summary>
        /// Adds a product line with a chosen size and toppings. The unit
        /// price is the final per-item price (size price + toppings).
        /// </summary>
        public void AddConfigured(
            Guid productId,
            string productName,
            string productCode,
            decimal unitPrice,
            int quantity,
            Guid? sizeOptionId,
            string sizeName,
            List<Guid> toppingIds,
            string toppingNames,
            decimal toppingsTotal,
            bool isCombo,
            string note = null,
            string imageUrl = null)
        {
            toppingIds ??= new List<Guid>();
            var key = BuildLineKey(productId, sizeOptionId, toppingIds, isCombo);

            var existing = _items.FirstOrDefault(x => x.LineKey == key);
            if (existing == null)
            {
                _items.Add(new CartItemModel
                {
                    LineKey = key,
                    ProductId = productId,
                    ProductName = productName,
                    ProductCode = productCode,
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    SizeOptionId = sizeOptionId,
                    SizeName = sizeName,
                    ToppingIds = toppingIds,
                    ToppingNames = toppingNames,
                    ToppingsTotal = toppingsTotal,
                    IsCombo = isCombo,
                    Note = TrimNote(note),
                    ImageUrl = imageUrl
                });
            }
            else
            {
                existing.Quantity += quantity;
                if (!string.IsNullOrWhiteSpace(note))
                {
                    existing.Note = TrimNote(note);
                }
                // Backfill images for lines saved before quick-add carried
                // the product image (old localStorage carts).
                if (string.IsNullOrWhiteSpace(existing.ImageUrl)
                    && !string.IsNullOrWhiteSpace(imageUrl))
                {
                    existing.ImageUrl = imageUrl;
                }
            }

            NotifyChanged();
        }

        /// <summary>
        /// Wave 6a (item 24): updates the per-item "ghi chú món" on a cart
        /// line (persists to localStorage via <see cref="NotifyChanged"/>).
        /// </summary>
        public void UpdateNote(string lineKey, string note)
        {
            var existing = _items.FirstOrDefault(x => x.LineKey == lineKey);
            if (existing == null)
            {
                return;
            }

            var trimmed = TrimNote(note);
            if (existing.Note != trimmed)
            {
                existing.Note = trimmed;
                NotifyChanged();
            }
        }

        private static string TrimNote(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return null;
            }

            var trimmed = note.Trim();
            return trimmed.Length > 500 ? trimmed.Substring(0, 500) : trimmed;
        }

        /// <summary>
        /// First non-empty URL from the product's ImageUrlsJson array
        /// (same parse as the shop/cart Razor pages).
        /// </summary>
        private static string FirstImageUrl(string imageUrlsJson)
        {
            if (string.IsNullOrWhiteSpace(imageUrlsJson))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(imageUrlsJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        if (el.ValueKind == JsonValueKind.String)
                        {
                            var url = el.GetString()?.Trim();
                            if (!string.IsNullOrEmpty(url))
                            {
                                return url;
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Corrupt JSON: treat as no image.
            }

            return null;
        }

        /// <summary>Adds a family combo as a single line at the combo price.</summary>
        public void AddCombo(ComboModel combo, int quantity = 1)
        {
            AddConfigured(
                combo.Id, combo.Name, "COMBO", combo.Price, quantity,
                sizeOptionId: null, sizeName: null,
                toppingIds: null, toppingNames: null, toppingsTotal: 0,
                isCombo: true);
        }

        public void UpdateQuantity(string lineKey, int quantity)
        {
            var existing = _items.FirstOrDefault(x => x.LineKey == lineKey);
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

        public void UpdateQuantity(Guid productId, int quantity)
        {
            var existing = _items.FirstOrDefault(x => x.ProductId == productId);
            if (existing == null)
            {
                return;
            }

            UpdateQuantity(existing.LineKey, quantity);
        }

        public void Remove(string lineKey)
        {
            _items.RemoveAll(x => x.LineKey == lineKey);
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

        public static string BuildLineKey(
            Guid productId,
            Guid? sizeOptionId,
            IEnumerable<Guid> toppingIds,
            bool isCombo)
        {
            var toppings = toppingIds == null
                ? string.Empty
                : string.Join(",", toppingIds.OrderBy(x => x).Select(x => x.ToString("N")));
            return $"{(isCombo ? "combo" : "product")}:{productId:N}:{sizeOptionId?.ToString("N")}:{toppings}";
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
