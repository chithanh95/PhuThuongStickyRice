using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Services
{
    /// <summary>
    /// Dev/test SMS sender: writes the message to the log instead of calling
    /// a real SMS gateway. Used so the forgot-password OTP flow can be tested
    /// end-to-end (read the OTP from the API logs) before the shop contracts
    /// a real provider. No credentials are stored or sent anywhere.
    /// </summary>
    public class LoggingSmsSender : ISmsSender
    {
        private readonly ILogger<LoggingSmsSender> _logger;
        private readonly SmsOptions _options;

        public LoggingSmsSender(ILogger<LoggingSmsSender> logger, SmsOptions options)
        {
            _logger = logger;
            _options = options;
        }

        public Task SendAsync(string phone, string message, CancellationToken cancellationToken = default)
        {
            if (!_options.IsConfigured)
            {
                _logger.LogInformation(
                    "[SMS:Logging] Chưa cấu hình SMS provider thật (Sms:ApiKey còn placeholder) — " +
                    "ghi log thay vì gửi. To: {Phone}. Nội dung: {Message}",
                    phone, message);
            }
            else
            {
                _logger.LogInformation("[SMS:{Provider}] To: {Phone}. Nội dung: {Message}",
                    _options.Provider, phone, message);
            }

            return Task.CompletedTask;
        }
    }
}
