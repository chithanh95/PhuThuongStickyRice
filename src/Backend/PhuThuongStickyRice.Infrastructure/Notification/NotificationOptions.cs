using PhuThuongStickyRice.Infrastructure.Notification.Email;
using PhuThuongStickyRice.Infrastructure.Notification.Sms;
using PhuThuongStickyRice.Infrastructure.Notification.Web;

namespace PhuThuongStickyRice.Infrastructure.Notification
{
    public class NotificationOptions
    {
        public EmailOptions Email { get; set; }

        public SmsOptions Sms { get; set; }

        public WebOptions Web { get; set; }
    }
}
