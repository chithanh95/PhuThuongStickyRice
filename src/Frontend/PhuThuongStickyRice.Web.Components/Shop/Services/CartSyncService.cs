using Microsoft.AspNetCore.Components.Authorization;
using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Item 137: syncs the cart with the server for logged-in users.
    /// Server is the source of truth when logged in; localStorage otherwise.
    /// Simple: on login, if server has a cart, use it. On change, save debounced.
    /// </summary>
    public class CartSyncService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly CartState _cartState;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ITokenManager _tokenManager;
        private Timer _debounceTimer;
        // 2 giây: đủ lâu để gộp nhiều lần bấm +/- liên tiếp thành 1 lần lưu,
        // đủ nhanh để giỏ vẫn được lưu nếu user tắt tab ngay sau khi sửa.
        private const int DebounceMs = 2000;

        public CartSyncService(
            HttpClient httpClient,
            CartState cartState,
            AuthenticationStateProvider authStateProvider,
            ITokenManager tokenManager)
        {
            _httpClient = httpClient;
            _cartState = cartState;
            _authStateProvider = authStateProvider;
            _tokenManager = tokenManager;
            _cartState.OnChange += OnCartChanged;
        }

        private async Task SetBearerTokenAsync()
        {
            try
            {
                var tokenModel = await _tokenManager.GetToken();
                var token = tokenModel?.AccessToken;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch
            {
                // Token lookup is best-effort; the request proceeds without an auth header.
            }
        }

        private async Task<bool> IsLoggedInAsync()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                return authState.User.Identity?.IsAuthenticated == true;
            }
            catch
            {
                // An toàn khi bỏ qua: không đọc được trạng thái đăng nhập thì
                // coi như chưa đăng nhập — giỏ hàng local vẫn hoạt động bình
                // thường (fail-safe, không chặn user mua hàng).
                return false;
            }
        }

        /// <summary>
        /// Gọi sau khi đăng nhập: nếu server có giỏ hàng thì thay giỏ local
        /// bằng giỏ server (server thắng — vì user có thể đã thêm món trên
        /// thiết bị khác; giỏ local chỉ dùng khi chưa đăng nhập).
        /// </summary>
        public async Task SyncOnLoginAsync()
        {
            if (!await IsLoggedInAsync())
            {
                return;
            }

            try
            {
                await SetBearerTokenAsync();
                using var response = await _httpClient.GetAsync("api/cart");
                if (!response.IsSuccessStatusCode)
                {
                    // Fail-soft: giữ giỏ local, không chặn user mua hàng.
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("itemsJson", out var itemsProp))
                {
                    var itemsJson = itemsProp.GetString();
                    if (!string.IsNullOrWhiteSpace(itemsJson))
                    {
                        var items = JsonSerializer.Deserialize<List<CartItemModel>>(itemsJson);
                        if (items != null && items.Count > 0)
                        {
                            // Server wins: clear local and load server items.
                            _cartState.Clear();
                            foreach (var item in items)
                            {
                                // Bỏ qua món hỏng (null hoặc số lượng <= 0) để không
                                // làm sập giỏ khi dữ liệu server bị lệch.
                                if (item != null && item.Quantity > 0)
                                {
                                    _cartState.AddConfigured(
                                        item.ProductId, item.ProductName, item.ProductCode,
                                        item.UnitPrice, item.Quantity,
                                        item.SizeOptionId, item.SizeName,
                                        item.ToppingIds, item.ToppingNames, item.ToppingsTotal,
                                        item.IsCombo, item.Note, item.ImageUrl);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fail-soft: keep local cart.
            }
        }

        private void OnCartChanged()
        {
            // Debounce: reset timer mỗi lần giỏ thay đổi, chỉ lưu server sau
            // 2s yên lặng — tránh spam PUT api/cart mỗi lần bấm +/- số lượng.
            // Fire-and-forget bằng discard Task (không dùng async void):
            // TimerCallback là void nên lambda async sẽ thành async void —
            // nếu có exception thoát ra sẽ crash process trên threadpool.
            // Discard Task thì exception (không thể xảy ra vì
            // SaveToServerAsync đã try/catch toàn bộ) chỉ thành unobserved.
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ => _ = SaveToServerAsync(), null, DebounceMs, Timeout.Infinite);
        }

        private async Task SaveToServerAsync()
        {
            if (!await IsLoggedInAsync())
            {
                return;
            }

            try
            {
                await SetBearerTokenAsync();
                var itemsJson = JsonSerializer.Serialize(_cartState.Items);
                var content = new StringContent(
                    JsonSerializer.Serialize(new { ItemsJson = itemsJson }),
                    Encoding.UTF8, "application/json");
                // Dispose response để trả connection về pool ngay sau khi PUT xong.
                using var response = await _httpClient.PutAsync("api/cart", content);
            }
            catch
            {
                // Fail-soft: local cart still works.
            }
        }

        /// <summary>
        /// Dọn dẹp: hủy timer debounce và gỡ lắng nghe thay đổi giỏ để
        /// tránh rò rỉ bộ nhớ (CartState sống lâu hơn service này).
        /// </summary>
        public void Dispose()
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
            _cartState.OnChange -= OnCartChanged;
            GC.SuppressFinalize(this);
        }
    }
}
