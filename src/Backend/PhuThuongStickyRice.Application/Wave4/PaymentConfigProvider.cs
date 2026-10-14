using PhuThuongStickyRice.Application.Orders.Payments;
using PhuThuongStickyRice.Application.Shop;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PhuThuongStickyRice.Application.Wave4
{
    /// <summary>
    /// Payment config resolution (item 106): reads the non-secret display
    /// values from <c>ShopSetting</c> first (editable at /admin/thanh-toan),
    /// falling back to appsettings.json / appsettings.Production.json.
    /// Secrets (MoMo AccessKey/SecretKey, TestMode, URLs) always come from
    /// appsettings and can never be overridden from the database.
    /// </summary>
    public interface IPaymentConfigProvider : ITransientDependency
    {
        Task<BankTransferOptions> GetBankTransferAsync();

        Task<MoMoOptions> GetMoMoAsync();
    }

    public class PaymentConfigProvider : IPaymentConfigProvider
    {
        private readonly IOptions<BankTransferOptions> _bankTransferDefaults;
        private readonly IOptions<MoMoOptions> _momoDefaults;
        private readonly IShopSettingAppService _shopSettings;

        public PaymentConfigProvider(
            IOptions<BankTransferOptions> bankTransferDefaults,
            IOptions<MoMoOptions> momoDefaults,
            IShopSettingAppService shopSettings)
        {
            _bankTransferDefaults = bankTransferDefaults;
            _momoDefaults = momoDefaults;
            _shopSettings = shopSettings;
        }

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

            var db = await _shopSettings.GetAsync();
            if (!string.IsNullOrWhiteSpace(db.BankName))
            {
                merged.BankName = db.BankName;
            }

            if (!string.IsNullOrWhiteSpace(db.BankId))
            {
                merged.BankId = db.BankId;
            }

            if (!string.IsNullOrWhiteSpace(db.BankAccountNumber))
            {
                merged.AccountNumber = db.BankAccountNumber;
            }

            if (!string.IsNullOrWhiteSpace(db.BankAccountName))
            {
                merged.AccountName = db.BankAccountName;
            }

            if (!string.IsNullOrWhiteSpace(db.BankQrTemplate))
            {
                merged.QrTemplate = db.BankQrTemplate;
            }

            return merged;
        }

        public async Task<MoMoOptions> GetMoMoAsync()
        {
            var defaults = _momoDefaults.Value;
            var merged = new MoMoOptions
            {
                // Non-secret values MAY come from the database.
                PartnerCode = defaults.PartnerCode,
                Endpoint = defaults.Endpoint,

                // Secrets + operational flags ALWAYS come from appsettings.
                AccessKey = defaults.AccessKey,
                SecretKey = defaults.SecretKey,
                TestMode = defaults.TestMode,
                StorefrontBaseUrl = defaults.StorefrontBaseUrl,
                ApiBaseUrl = defaults.ApiBaseUrl
            };

            var db = await _shopSettings.GetAsync();
            if (!string.IsNullOrWhiteSpace(db.MoMoPartnerCode))
            {
                merged.PartnerCode = db.MoMoPartnerCode;
            }

            if (!string.IsNullOrWhiteSpace(db.MoMoEndpoint))
            {
                merged.Endpoint = db.MoMoEndpoint;
            }

            return merged;
        }
    }
}
