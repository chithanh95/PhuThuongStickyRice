using PhuThuongStickyRice.Domain.Notification;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Notification.Sms.Fake
{
    public class FakeSmsNotification : ISmsNotification
    {
        public Task SendAsync(ISmsMessage smsMessage, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
