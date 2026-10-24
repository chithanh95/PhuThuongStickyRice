using PhuThuongStickyRice.Application.Orders.Payments;
using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Orders.Email
{
    /// <summary>
    /// Builds the order-confirmation email (Vietnamese). The email is queued
    /// as an <see cref="EmailMessage"/> row via the existing pipeline and
    /// delivered by the BackgroundServer worker through the configured
    /// provider (Gmail SMTP — needs the App Password env var, see README).
    /// </summary>
    public static class OrderConfirmationEmailComposer
    {
        /// <summary>
        /// Địa chỉ người gửi trên mail xác nhận đơn: Gmail chính thức của
        /// quán. Phải trùng tài khoản SMTP đã cấu hình (Gmail chỉ cho gửi
        /// từ chính tài khoản đã đăng nhập hoặc alias đã xác thực) — đổi
        /// tài khoản gửi thì sửa cả ở đây lẫn Notification:Email:SmtpClient
        /// trong appsettings của BackgroundServer.
        /// </summary>
        public const string FromAddress = "Maihuyen280579@gmail.com";

        /// <summary>
        /// Dựng email xác nhận đơn hàng (HTML tiếng Việt).
        /// Hướng dẫn thanh toán chỉ đính kèm khi đơn là chuyển khoản mà chưa
        /// thanh toán — đơn MoMo/COD hoặc đã trả tiền không cần phần này.
        /// Toàn bộ dữ liệu do khách nhập đều được <see cref="Escape"/> trước khi
        /// nhúng vào HTML để chống chèn mã độc vào email.
        /// </summary>
        /// <param name="order">Đơn hàng vừa tạo (đã có Lines, các tổng tiền, phương thức thanh toán).</param>
        /// <param name="bankTransfer">Thông tin tài khoản shop; null thì bỏ qua bảng tài khoản.</param>
        /// <returns>Cặp (tiêu đề, thân HTML) để pipeline email đưa vào hàng đợi gửi.</returns>
        public static (string Subject, string Body) Compose(Order order, BankTransferOptions bankTransfer)
        {
            var subject = $"Xác nhận đơn hàng {order.OrderNumber} — Xôi Huyền";

            var orderedAt = VietnamTime.ToVietnamTime(order.CreatedAt);
            var body = new StringBuilder();
            body.Append("<html><body style=\"font-family: Arial, sans-serif; color: #222;\">");
            body.Append("<h2>Xôi Huyền — Xác nhận đơn hàng</h2>");
            body.Append($"<p>Cảm ơn <strong>{Escape(order.CustomerName)}</strong> đã đặt hàng!</p>");
            body.Append("<h3>Thông tin đơn hàng</h3>");
            AppendOrderInfoTable(body, order, orderedAt);

            body.Append("<h3>Chi tiết món</h3>");
            AppendLinesTable(body, order);

            // Chỉ đơn chuyển khoản CHƯA thanh toán mới cần hướng dẫn chuyển khoản —
            // đơn MoMo/COD có luồng thanh toán riêng, đơn đã trả tiền không cần hướng dẫn nữa.
            if (order.PaymentMethod == PaymentMethod.BankTransfer && order.PaymentStatus == PaymentStatus.Unpaid)
            {
                AppendBankTransferInstructions(body, order, bankTransfer);
            }

            if (order.PaymentMethod == PaymentMethod.Cod)
            {
                body.Append("<p>Bạn sẽ thanh toán bằng tiền mặt khi nhận hàng.</p>");
            }

            if (order.PaymentMethod == PaymentMethod.MoMo)
            {
                body.Append("<p>Vui lòng hoàn tất thanh toán qua MoMo theo hướng dẫn trên trang đặt hàng.</p>");
            }

            AppendFooter(body);
            body.Append("</body></html>");

            return (subject, body.ToString());
        }

        /// <summary>
        /// Bảng thông tin đơn hàng: mã đơn, ngày giờ, khách, địa chỉ, trạng
        /// thái đơn / thanh toán, phương thức thanh toán, ghi chú (nếu có).
        /// </summary>
        private static void AppendOrderInfoTable(StringBuilder body, Order order, DateTime orderedAt)
        {
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
        }

        /// <summary>
        /// Bảng chi tiết từng món + các dòng tổng: tạm tính, giảm giá (nếu có),
        /// phí giao hàng (nếu có), tổng tiền.
        /// </summary>
        private static void AppendLinesTable(StringBuilder body, Order order)
        {
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

            // Tạm tính không lưu riêng trong DB — suy ngược từ các tổng đã lưu.
            var subtotal = order.TotalAmount.Amount - order.DeliveryFee.Amount + order.DiscountAmount.Amount;
            body.Append($"<tr><td colspan=\"3\" align=\"right\">Tạm tính</td>"
                + $"<td align=\"right\">{FormatPrice(subtotal)}</td></tr>");
            if (order.DiscountAmount.Amount > 0)
            {
                var codeLabel = string.IsNullOrWhiteSpace(order.DiscountCode) ? "" : $" ({Escape(order.DiscountCode)})";
                body.Append($"<tr><td colspan=\"3\" align=\"right\">Giảm giá{codeLabel}</td>"
                    + $"<td align=\"right\">-{FormatPrice(order.DiscountAmount.Amount)}</td></tr>");
            }

            if (order.DeliveryFee.Amount > 0)
            {
                body.Append("<tr><td colspan=\"3\" align=\"right\">Phí giao hàng</td>"
                    + $"<td align=\"right\">{FormatPrice(order.DeliveryFee.Amount)}</td></tr>");
            }

            body.Append("<tr><td colspan=\"3\" align=\"right\"><strong>Tổng tiền</strong></td>"
                + $"<td align=\"right\"><strong>{FormatPrice(order.TotalAmount.Amount)}</strong></td></tr>");
            body.Append("</table>");
        }

        /// <summary>
        /// Hướng dẫn chuyển khoản cho đơn BankTransfer chưa thanh toán: nội
        /// dung chuyển khoản (mã đơn) + thông tin tài khoản shop (nếu có).
        /// </summary>
        private static void AppendBankTransferInstructions(StringBuilder body, Order order, BankTransferOptions bankTransfer)
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

        /// <summary>Chân email: thương hiệu + lưu ý email tự động.</summary>
        private static void AppendFooter(StringBuilder body)
        {
            body.Append("<hr/>");
            body.Append("<p><small>Xôi Huyền — Xôi làng nghề Phú Thượng, Hà Nội.<br/>"
                + "Đây là email tự động, vui lòng không trả lời email này.</small></p>");
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

        /// <summary>
        /// Escape dữ liệu do khách nhập trước khi nhúng vào HTML email — chống
        /// chèn thẻ/script độc hại (tên, địa chỉ, ghi chú đều là input tự do).
        /// </summary>
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
    }
}
