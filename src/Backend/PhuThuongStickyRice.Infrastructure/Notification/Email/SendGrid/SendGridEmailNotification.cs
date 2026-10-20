using PhuThuongStickyRice.Domain.Notification;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Notification.Email.SendGrid
{
    public class SendGridEmailNotification : IEmailNotification
    {
        private readonly SendGridOptions _options;

        // SendGridClient bọc HttpClient — giữ 1 instance dùng chung cho mọi
        // lần gửi (thread-safe theo thiết kế SDK, khuyến nghị tái sử dụng)
        // thay vì new mỗi email gây socket exhaustion khi worker gửi liên tục.
        private readonly SendGridClient _client;

        public SendGridEmailNotification(SendGridOptions options)
        {
            _options = options;
            _client = new SendGridClient(_options.ApiKey);
        }

        public async Task SendAsync(IEmailMessage emailMessage, CancellationToken cancellationToken = default)
        {
            var from = new EmailAddress(!string.IsNullOrWhiteSpace(_options.OverrideFrom) ? _options.OverrideFrom : emailMessage.From);

            var tos = (!string.IsNullOrWhiteSpace(_options.OverrideTos) ? _options.OverrideTos : emailMessage.Tos)?.Split(';')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new EmailAddress(x))
                .ToList();

            // Fail-fast với message rõ ràng thay vì NullReferenceException sâu
            // trong helper của SendGrid khi Tos null/rỗng.
            if (tos == null || tos.Count == 0)
            {
                throw new ArgumentException("Email phải có ít nhất một người nhận (Tos).", nameof(emailMessage));
            }

            var mail = MailHelper.CreateSingleEmailToMultipleRecipients(from, tos, emailMessage.Subject, string.Empty, emailMessage.Body, showAllRecipients: true);
            await _client.SendEmailAsync(mail, cancellationToken);
        }
    }
}
