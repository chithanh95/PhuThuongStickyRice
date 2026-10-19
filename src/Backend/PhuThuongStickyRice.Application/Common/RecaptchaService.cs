using PhuThuongStickyRice.Application.Shop;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PhuThuongStickyRice.Application.Common
{
    // codequality-team 2026-10-20 run-0245 Agent4-Docs
    /// <summary>
    /// Item 96: xác minh reCAPTCHA v3 ở phía server (chống bot spam đơn).
    /// Khi admin chưa cấu hình secret key thì tính năng bị tắt và luôn pass
    /// (trả true) để đơn hàng vẫn chạy được.
    /// </summary>
    public interface IRecaptchaService : ITransientDependency
    {
        /// <summary>
        /// Xác minh token reCAPTCHA: true khi đạt score tối thiểu 0.5
        /// (ngưỡng Google khuyến nghị), hoặc khi reCAPTCHA chưa được cấu hình.
        /// </summary>
        Task<bool> VerifyAsync(string token, string remoteIp = null);
    }

    /// <summary>
    /// Item 96: xác minh reCAPTCHA v3 ở phía server (chống bot spam đơn).
    /// Secret key lấy từ cài đặt shop của admin — bản public của cài đặt
    /// che secret, chỉ server đọc được đầy đủ. Khi admin chưa cấu hình
    /// secret thì xác minh bị tắt (luôn pass) để đơn vẫn chạy được.
    /// Xác minh thất bại hoặc lỗi mạng đều trả về false (fail closed):
    /// đã bật reCAPTCHA thì bot không được lọt qua khi Google không phản hồi.
    /// </summary>
    public class RecaptchaService : IRecaptchaService
    {
        /// <summary>
        /// Ngưỡng score reCAPTCHA v3 tối thiểu (0–1) để coi là người thật.
        /// Google trả score càng cao càng giống người; 0.5 là ngưỡng
        /// mặc định Google khuyến nghị cho form đặt hàng.
        /// </summary>
        private const double MinimumScore = 0.5;

        private readonly HttpClient _httpClient;
        private readonly IShopSettingAppService _shopSettings;

        /// <summary>
        /// Khởi tạo service xác minh reCAPTCHA.
        /// </summary>
        /// <param name="httpClient">HTTP client gọi API siteverify của Google.</param>
        /// <param name="shopSettings">Service cài đặt shop (đọc secret key reCAPTCHA).</param>
        public RecaptchaService(HttpClient httpClient, IShopSettingAppService shopSettings)
        {
            _httpClient = httpClient;
            _shopSettings = shopSettings;
        }

        /// <summary>
        /// Xác minh token reCAPTCHA của client với Google. Trả về true khi
        /// secret chưa cấu hình (tính năng bị tắt) hoặc token đạt score ≥ 0.5;
        /// false khi token rỗng, Google trả success=false, score thấp, hoặc
        /// lỗi mạng (fail closed để bot không lọt qua).
        /// </summary>
        /// <param name="token">Token reCAPTCHA client gửi lên.</param>
        /// <param name="remoteIp">IP client (tùy chọn, gửi kèm cho Google chấm điểm chính xác hơn).</param>
        /// <returns>True khi xác minh đạt (hoặc tính năng bị tắt).</returns>
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

            // FormUrlEncodedContent là IDisposable (giữ buffer đã encode):
            // dispose sau khi request hoàn tất, tránh rò rỉ tài nguyên.
            using var form = new FormUrlEncodedContent(new[]
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

                return IsScorePassing(root);
            }
            catch (Exception)
            {
                // Verification unavailable: fail closed so bots can't slip
                // through when the admin enabled reCAPTCHA.
                return false;
            }
        }

        /// <summary>
        /// True khi score reCAPTCHA v3 đạt ngưỡng tối thiểu; phản hồi v2/
        /// checkbox không có score thì coi như pass (fail-open cho dạng này
        /// vì Google trả success=true mà không kèm score).
        /// </summary>
        private static bool IsScorePassing(JsonElement root)
        {
            // v3 returns a score; v2/checkbox has no score — treat as pass.
            if (root.TryGetProperty("score", out var score))
            {
                return score.GetDouble() >= MinimumScore;
            }

            return true;
        }
    }
}
