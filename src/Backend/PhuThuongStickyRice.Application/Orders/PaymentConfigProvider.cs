using PhuThuongStickyRice.Application.Orders.Payments;
using PhuThuongStickyRice.Application.Shop;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Giải quyết cấu hình thanh toán (item 106): giá trị hiển thị KHÔNG phải
    /// bí mật đọc từ <c>ShopSetting</c> trước (sửa được ở /admin/thanh-toan),
    /// rồi fallback về appsettings.json / appsettings.Production.json.
    /// Bí mật (MoMo AccessKey/SecretKey, TestMode, URLs) LUÔN chỉ lấy từ
    /// appsettings, không bao giờ được ghi đè từ database — để bảo mật
    /// (ai chiếm quyền admin cũng không đổi được khóa ký).
    /// </summary>
    public interface IPaymentConfigProvider : ITransientDependency
    {
        /// <summary>
        /// Cấu hình chuyển khoản ngân hàng hiển thị ở checkout (tên NH, số TK,
        /// chủ TK, mẫu QR): giá trị DB thắng khi đã cấu hình, còn lại dùng
        /// appsettings làm mặc định.
        /// </summary>
        /// <returns>Cấu hình chuyển khoản đã gộp DB + appsettings.</returns>
        Task<BankTransferOptions> GetBankTransferAsync();

        /// <summary>
        /// Cấu hình MoMo: PartnerCode và Endpoint MAY được từ DB; AccessKey,
        /// SecretKey, TestMode và các URL LUÔN chỉ từ appsettings (không ghi
        /// đè từ DB vì lý do bảo mật).
        /// </summary>
        /// <returns>Cấu hình MoMo đã gộp.</returns>
        Task<MoMoOptions> GetMoMoAsync();
    }

    public class PaymentConfigProvider : IPaymentConfigProvider
    {
        private readonly IOptions<BankTransferOptions> _bankTransferDefaults;
        private readonly IOptions<MoMoOptions> _momoDefaults;
        private readonly IShopSettingAppService _shopSettings;

        /// <summary>Khởi tạo provider cấu hình thanh toán (đọc mặc định từ appsettings, ghi đè hiển thị từ DB).</summary>
        public PaymentConfigProvider(
            IOptions<BankTransferOptions> bankTransferDefaults,
            IOptions<MoMoOptions> momoDefaults,
            IShopSettingAppService shopSettings)
        {
            _bankTransferDefaults = bankTransferDefaults;
            _momoDefaults = momoDefaults;
            _shopSettings = shopSettings;
        }

        /// <summary>
        /// Cấu hình chuyển khoản ngân hàng hiển thị ở checkout: giá trị DB
        /// thắng khi admin đã cấu hình, còn lại dùng appsettings.
        /// </summary>
        /// <returns>Cấu hình chuyển khoản đã gộp DB + appsettings.</returns>
        public async Task<BankTransferOptions> GetBankTransferAsync()
        {
            var defaults = _bankTransferDefaults.Value;
            var merged = new BankTransferOptions
            {
                BankName = defaults.BankName,
                BankId = defaults.BankId,
                AccountNumber = defaults.AccountNumber,
                AccountName = defaults.AccountName,
                QrTemplate = defaults.QrTemplate
            };

            var shopSettings = await _shopSettings.GetAsync();
            if (!string.IsNullOrWhiteSpace(shopSettings.BankName))
            {
                merged.BankName = shopSettings.BankName;
            }

            if (!string.IsNullOrWhiteSpace(shopSettings.BankId))
            {
                merged.BankId = shopSettings.BankId;
            }

            if (!string.IsNullOrWhiteSpace(shopSettings.BankAccountNumber))
            {
                merged.AccountNumber = shopSettings.BankAccountNumber;
            }

            if (!string.IsNullOrWhiteSpace(shopSettings.BankAccountName))
            {
                merged.AccountName = shopSettings.BankAccountName;
            }

            if (!string.IsNullOrWhiteSpace(shopSettings.BankQrTemplate))
            {
                merged.QrTemplate = shopSettings.BankQrTemplate;
            }

            return merged;
        }

        /// <summary>
        /// Cấu hình MoMo: PartnerCode và Endpoint MAY được từ DB;
        /// AccessKey, SecretKey, TestMode và URLs LUÔN chỉ từ appsettings
        /// (không ghi đè từ DB vì lý do bảo mật).
        /// </summary>
        /// <returns>Cấu hình MoMo đã gộp.</returns>
        public async Task<MoMoOptions> GetMoMoAsync()
        {
            var defaults = _momoDefaults.Value;
            var merged = new MoMoOptions
            {
                // Giá trị KHÔNG phải bí mật MAY được từ database.
                PartnerCode = defaults.PartnerCode,
                Endpoint = defaults.Endpoint,

                // Bí mật + cờ vận hành LUÔN chỉ từ appsettings (không ghi đè từ DB).
                AccessKey = defaults.AccessKey,
                SecretKey = defaults.SecretKey,
                TestMode = defaults.TestMode,
                StorefrontBaseUrl = defaults.StorefrontBaseUrl,
                ApiBaseUrl = defaults.ApiBaseUrl
            };

            var shopSettings = await _shopSettings.GetAsync();
            if (!string.IsNullOrWhiteSpace(shopSettings.MoMoPartnerCode))
            {
                merged.PartnerCode = shopSettings.MoMoPartnerCode;
            }

            if (!string.IsNullOrWhiteSpace(shopSettings.MoMoEndpoint))
            {
                merged.Endpoint = shopSettings.MoMoEndpoint;
            }

            return merged;
        }
    }
}
