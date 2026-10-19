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
    public class CartSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly CartState _cartState;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ITokenManager _tokenManager;
        private Timer _debounceTimer;
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
            catch { }
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
                return false;
            }
        }

        /// <summary>
        /// Called after login: if server has a cart, replace local cart with it.
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
                var response = await _httpClient.GetAsync("api/cart");
                if (!response.IsSuccessStatusCode)
                {
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
            // Debounce server saves.
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(async _ => await SaveToServerAsync(), null, DebounceMs, Timeout.Infinite);
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
                await _httpClient.PutAsync("api/cart", content);
            }
            catch
            {
                // Fail-soft: local cart still works.
            }
        }
    }
}
