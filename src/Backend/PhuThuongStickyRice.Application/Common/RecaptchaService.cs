using PhuThuongStickyRice.Application.Shop;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PhuThuongStickyRice.Application.Common
{
    /// <summary>
    /// Item 96: server-side reCAPTCHA v3 verification.
    /// Disabled (returns true) when no secret key is configured.
    /// </summary>
    public interface IRecaptchaService : ITransientDependency
    {
        /// <summary>
        /// True when the token verifies with a score of at least 0.5.
        /// Returns true when reCAPTCHA is not configured (disabled).
        /// </summary>
        Task<bool> VerifyAsync(string token, string remoteIp = null);
    }

    public class RecaptchaService : IRecaptchaService
    {
        private const double MinimumScore = 0.5;

        private readonly HttpClient _httpClient;
        private readonly IShopSettingAppService _shopSettings;

        public RecaptchaService(HttpClient httpClient, IShopSettingAppService shopSettings)
        {
            _httpClient = httpClient;
            _shopSettings = shopSettings;
        }

        public async Task<bool> VerifyAsync(string token, string remoteIp = null)
        {
            var settings = await _shopSettings.GetAsync();
            if (string.IsNullOrWhiteSpace(settings.RecaptchaSecretKey))
            {
                // reCAPTCHA not configured: verification is disabled.
                return true;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", settings.RecaptchaSecretKey),
                new KeyValuePair<string, string>("response", token),
                new KeyValuePair<string, string>("remoteip", remoteIp ?? string.Empty)
            });

            try
            {
                using var response = await _httpClient.PostAsync(
                    "https://www.google.com/recaptcha/api/siteverify", form);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("success", out var success) || !success.GetBoolean())
                {
                    return false;
                }

                // v3 returns a score; v2/checkbox has no score — treat as pass.
                if (root.TryGetProperty("score", out var score))
                {
                    return score.GetDouble() >= MinimumScore;
                }

                return true;
            }
            catch (Exception)
            {
                // Verification unavailable: fail closed so bots can't slip
                // through when the admin enabled reCAPTCHA.
                return false;
            }
        }
    }
}
