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

        public SendGridEmailNotification(SendGridOptions options)
        {
            _options = options;
        }

        public async Task SendAsync(IEmailMessage emailMessage, CancellationToken cancellationToken = default)
        {
            var client = new SendGridClient(_options.ApiKey);
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
            await client.SendEmailAsync(mail, cancellationToken);
        }
    }
}
