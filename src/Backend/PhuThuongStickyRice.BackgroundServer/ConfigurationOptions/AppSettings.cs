using PhuThuongStickyRice.Infrastructure.Logging;
using PhuThuongStickyRice.Infrastructure.Notification;
using Microsoft.Extensions.Options;

namespace PhuThuongStickyRice.BackgroundServer.ConfigurationOptions
{
    public class AppSettings
    {
        public ConnectionStrings ConnectionStrings { get; set; }

        public LoggingOptions Logging { get; set; }

        public string AllowedHosts { get; set; }

        public string CurrentUrl { get; set; }

        public NotificationOptions Notification { get; set; }

        public ValidateOptionsResult Validate()
        {
            return ValidateOptionsResult.Success;
        }
    }

    public class AppSettingsValidation : IValidateOptions<AppSettings>
    {
        public ValidateOptionsResult Validate(string name, AppSettings options)
        {
            return options.Validate();
        }
    }
}
