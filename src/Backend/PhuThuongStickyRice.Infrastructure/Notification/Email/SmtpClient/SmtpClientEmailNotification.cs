using PhuThuongStickyRice.Domain.Notification;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Notification.Email.SmtpClient
{
    public class SmtpClientEmailNotification : IEmailNotification
    {
        private readonly SmtpClientOptions _options;

        public SmtpClientEmailNotification(SmtpClientOptions options)
        {
            _options = options;
        }

        public async Task SendAsync(IEmailMessage emailMessage, CancellationToken cancellationToken = default)
        {
            using var mail = new MailMessage();

            mail.From = new MailAddress(emailMessage.From);

            foreach (var to in (emailMessage.Tos?.Split(';') ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                mail.To.Add(to);
            }

            foreach (var cc in (emailMessage.CCs?.Split(';') ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                mail.CC.Add(cc);
            }

            foreach (var bcc in (emailMessage.BCCs?.Split(';') ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                mail.Bcc.Add(bcc);
            }

            mail.Subject = emailMessage.Subject;

            mail.Body = emailMessage.Body;

            mail.IsBodyHtml = true;

            using var smtpClient = new System.Net.Mail.SmtpClient(_options.Host);

            if (_options.Port.HasValue)
            {
                smtpClient.Port = _options.Port.Value;
            }

            if (!string.IsNullOrWhiteSpace(_options.UserName) && !string.IsNullOrWhiteSpace(_options.Password))
            {
                smtpClient.Credentials = new System.Net.NetworkCredential(_options.UserName, _options.Password);
            }

            if (_options.EnableSsl.HasValue)
            {
                smtpClient.EnableSsl = _options.EnableSsl.Value;
            }

            await smtpClient.SendMailAsync(mail, cancellationToken);
        }
    }
}
