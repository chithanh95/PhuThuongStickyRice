using PhuThuongStickyRice.Application.Orders.Payments;
using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PhuThuongStickyRice.Application.Orders.Email
{
    /// <summary>
    /// Builds the order-confirmation email (Vietnamese). The email is queued
    /// as an <see cref="EmailMessage"/> row via the existing pipeline; with
    /// the current Fake/test provider nothing is actually delivered.
    /// </summary>
    public static class OrderConfirmationEmailComposer
    {
        /// <summary>Sender shown on order emails. Test sender — replace when real mail is configured.</summary>
        public const string FromAddress = "no-reply@phuthuong.vn";

        public static (string Subject, string Body) Compose(Order order, BankTransferOptions bankTransfer)
        {
            var subject = $"Xác nhận đơn hàng {order.OrderNumber} — Xôi Phú Thượng";

            var orderedAt = ToVietnamTime(order.CreatedAt);
            var body = new StringBuilder();
            body.Append("<html><body style=\"font-family: Arial, sans-serif; color: #222;\">");
            body.Append("<h2>Xôi Phú Thượng — Xác nhận đơn hàng</h2>");
            body.Append($"<p>Cảm ơn <strong>{Escape(order.CustomerName)}</strong> đã đặt hàng!</p>");
            body.Append("<h3>Thông tin đơn hàng</h3>");
            body.Append("<table cellpadding=\"6\" cellspacing=\"0\" border=\"0\">");
            body.Append($"<tr><td><strong>Mã đơn hàng:</strong></td><td>{Escape(order.OrderNumber)}</td></tr>");
            body.Append($"<tr><td><strong>Ngày giờ đặt:</strong></td><td>{orderedAt:dd/MM/yyyy HH:mm} (giờ Việt Nam, UTC+7)</td></tr>");
            body.Append($"<tr><td><strong>Khách hàng:</strong></td><td>{Escape(order.CustomerName)}</td></tr>");
            body.Append($"<tr><td><strong>Số điện thoại:</strong></td><td>{Escape(order.CustomerPhone)}</td></tr>");
            body.Append($"<tr><td><strong>Địa chỉ giao hàng:</strong></td><td>{Escape(order.CustomerAddress)}</td></tr>");
            body.Append($"<tr><td><strong>Trạng thái đơn:</strong></td><td>{Escape(order.Status.ToString())}</td></tr>");
            body.Append($"<tr><td><strong>Phương thức thanh toán:</strong></td><td>{PaymentMethodLabel(order.PaymentMethod)}</td></tr>");
            body.Append($"<tr><td><strong>Trạng thái thanh toán:</strong></td><td>{PaymentStatusLabel(order.PaymentStatus)}</td></tr>");
            if (!string.IsNullOrWhiteSpace(order.Notes))
            {
                body.Append($"<tr><td><strong>Ghi chú:</strong></td><td>{Escape(order.Notes)}</td></tr>");
            }

            body.Append("</table>");

            body.Append("<h3>Chi tiết món</h3>");
            body.Append("<table cellpadding=\"6\" cellspacing=\"0\" border=\"1\" style=\"border-collapse: collapse;\">");
            body.Append("<tr><th align=\"left\">Món</th><th align=\"right\">Số lượng</th><th align=\"right\">Đơn giá</th><th align=\"right\">Thành tiền</th></tr>");
            foreach (var line in order.Lines)
            {
                body.Append("<tr>");
                body.Append($"<td>{Escape(line.ProductName)}</td>");
                body.Append($"<td align=\"right\">{line.Quantity}</td>");
                body.Append($"<td align=\"right\">{FormatPrice(line.UnitPrice.Amount)}</td>");
                body.Append($"<td align=\"right\">{FormatPrice(line.LineTotal.Amount)}</td>");
                body.Append("</tr>");
            }

            body.Append("<tr><td colspan=\"3\" align=\"right\"><strong>Tổng tiền</strong></td>"
                + $"<td align=\"right\"><strong>{FormatPrice(order.TotalAmount.Amount)}</strong></td></tr>");
            body.Append("</table>");

            if (order.PaymentMethod == PaymentMethod.BankTransfer && order.PaymentStatus == PaymentStatus.Unpaid)
            {
                body.Append("<h3>Hướng dẫn chuyển khoản</h3>");
                body.Append("<p>Vui lòng chuyển khoản với nội dung: "
                    + $"<strong>{Escape(order.OrderNumber)}</strong>. Sau khi chuyển, bấm "
                    + "“Tôi đã chuyển khoản” trên trang tra cứu đơn hàng để shop xác nhận.</p>");
                if (bankTransfer != null)
                {
                    body.Append("<table cellpadding=\"6\" cellspacing=\"0\" border=\"0\">");
                    body.Append($"<tr><td><strong>Ngân hàng:</strong></td><td>{Escape(bankTransfer.BankName)}</td></tr>");
                    body.Append($"<tr><td><strong>Số tài khoản:</strong></td><td>{Escape(bankTransfer.AccountNumber)}</td></tr>");
                    body.Append($"<tr><td><strong>Chủ tài khoản:</strong></td><td>{Escape(bankTransfer.AccountName)}</td></tr>");
                    body.Append($"<tr><td><strong>Số tiền:</strong></td><td>{FormatPrice(order.TotalAmount.Amount)}</td></tr>");
                    body.Append("</table>");
                }
            }

            if (order.PaymentMethod == PaymentMethod.Cod)
            {
                body.Append("<p>Bạn sẽ thanh toán bằng tiền mặt khi nhận hàng.</p>");
            }

            if (order.PaymentMethod == PaymentMethod.MoMo)
            {
                body.Append("<p>Vui lòng hoàn tất thanh toán qua MoMo theo hướng dẫn trên trang đặt hàng.</p>");
            }

            body.Append("<hr/>");
            body.Append("<p><small>Xôi Phú Thượng — Xôi làng nghề Phú Thượng, Hà Nội.<br/>"
                + "Đây là email tự động, vui lòng không trả lời email này.</small></p>");
            body.Append("</body></html>");

            return (subject, body.ToString());
        }

        private static string PaymentMethodLabel(PaymentMethod method) => method switch
        {
            PaymentMethod.Cod => "Thanh toán khi nhận hàng (COD)",
            PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
            PaymentMethod.MoMo => "Ví MoMo",
            _ => method.ToString()
        };

        private static string PaymentStatusLabel(PaymentStatus status) => status switch
        {
            PaymentStatus.Unpaid => "Chưa thanh toán",
            PaymentStatus.PendingConfirmation => "Chờ xác nhận thanh toán",
            PaymentStatus.Paid => "Đã thanh toán",
            PaymentStatus.Failed => "Thanh toán thất bại",
            _ => status.ToString()
        };

        private static string FormatPrice(decimal price) =>
            $"{price.ToString("N0", CultureInfo.InvariantCulture)} ₫";

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        /// <summary>
        /// Order.CreatedAt is UTC. Display it in Vietnam time (UTC+7).
        /// Tries the IANA id (Linux), then the Windows id, then a fixed +07 offset.
        /// </summary>
        internal static DateTime ToVietnamTime(DateTime utc)
        {
            var utcDateTime = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                    return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, tz);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return utcDateTime.AddHours(7);
        }
    }
}
