#nullable enable
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
    /// Item 137: đồng bộ giỏ hàng với server cho user đã đăng nhập.
    /// Khi đã đăng nhập thì server là nguồn đúng (localStorage chỉ dùng khi
    /// chưa đăng nhập). Đơn giản: sau login, nếu server có giỏ thì dùng giỏ
    /// server. Khi giỏ thay đổi thì lưu lên server (debounce).
    /// </summary>
    public class CartSyncService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly CartState _cartState;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ITokenStore _tokenStore;
        private Timer? _debounceTimer;
        // 2 giây: đủ lâu để gộp nhiều lần bấm +/- liên tiếp thành 1 lần lưu,
        // đủ nhanh để giỏ vẫn được lưu nếu user tắt tab ngay sau khi sửa.
        private const int DebounceMs = 2000;

        public CartSyncService(
            HttpClient httpClient,
            CartState cartState,
            AuthenticationStateProvider authStateProvider,
            ITokenStore tokenStore)
        {
            _httpClient = httpClient;
            _cartState = cartState;
            _authStateProvider = authStateProvider;
            _tokenStore = tokenStore;
            _cartState.OnChange += OnCartChanged;
        }

        private async Task SetBearerTokenAsync()
        {
            try
            {
                var tokenModel = await _tokenStore.GetToken();
                var token = tokenModel?.AccessToken;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
                else
                {
                    // bug-hunt 2026-10-19: token null/rỗng (đã đăng xuất) mà
                    // không xóa header → Bearer <redacted>ũ vẫn dính trên HttpClient
                    // dùng chung, request sau gửi nhầm identity phiên cũ.
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }
            }
            catch
            {
                // Đọc token là best-effort; không đọc được thì request vẫn
                // chạy tiếp không có header auth.
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
                var items = await FetchServerCartItemsAsync();
                if (items != null && items.Count > 0)
                {
                    ApplyServerCartItems(items);
                }
            }
            catch
            {
                // Fail-soft: giữ giỏ local, không chặn user mua hàng.
            }
        }

        /// <summary>
        /// Tải giỏ hàng đang lưu trên server của user. Trả về null khi server
        /// không có giỏ, request lỗi, hoặc payload không có itemsJson hợp lệ —
        /// caller lúc đó giữ nguyên giỏ local (fail-soft).
        /// </summary>
        private async Task<List<CartItemModel>?> FetchServerCartItemsAsync()
        {
            await SetBearerTokenAsync();
            using var response = await _httpClient.GetAsync("api/cart");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("itemsJson", out var itemsProp))
            {
                return null;
            }

            var itemsJson = itemsProp.GetString();
            if (string.IsNullOrWhiteSpace(itemsJson))
            {
                return null;
            }

            return JsonSerializer.Deserialize<List<CartItemModel>>(itemsJson);
        }

        /// <summary>
        /// Thay giỏ local bằng giỏ server (server thắng — user có thể đã thêm
        /// món trên thiết bị khác). Món hỏng (null hoặc số lượng &lt;= 0) bị bỏ
        /// qua để không làm sập giỏ khi dữ liệu server bị lệch.
        /// </summary>
        private void ApplyServerCartItems(List<CartItemModel> items)
        {
            _cartState.Clear();
            foreach (var item in items)
            {
                if (item == null || item.Quantity <= 0)
                {
                    continue;
                }

                _cartState.AddConfigured(
                    item.ProductId, item.ProductName, item.ProductCode,
                    item.UnitPrice, item.Quantity,
                    item.SizeOptionId, item.SizeName,
                    item.ToppingIds, item.ToppingNames, item.ToppingsTotal,
                    item.IsCombo, item.Note, item.ImageUrl);
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
                // StringContent giữ buffer/memory stream: dispose sau khi PUT xong.
                using var content = new StringContent(
                    JsonSerializer.Serialize(new { ItemsJson = itemsJson }),
                    Encoding.UTF8, "application/json");
                // Dispose response để trả connection về pool ngay sau khi PUT xong.
                using var response = await _httpClient.PutAsync("api/cart", content);
            }
            catch
            {
                // Fail-soft: giữ giỏ local, không chặn khách mua hàng khi
                // không lưu được lên server.
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
