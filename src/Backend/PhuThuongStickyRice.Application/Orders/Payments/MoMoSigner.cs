using System;
using System.Security.Cryptography;
using System.Text;

namespace PhuThuongStickyRice.Application.Orders.Payments
{
    /// <summary>
    /// MoMo HMAC-SHA256 signatures. The raw-string formats follow the MoMo
    /// v2 gateway docs; the same functions sign real requests and verify
    /// both real IPN callbacks and the simulated (test-mode) callback.
    /// </summary>
    public static class MoMoSigner
    {
        /// <summary>
        /// Ký chuỗi thô bằng HMAC-SHA256 với secret key của MoMo, trả về
        /// chuỗi hex lowercase. Dùng cho cả ký request tạo thanh toán lẫn
        /// xác thực chữ ký của callback IPN (thật và giả lập).
        /// </summary>
        /// <param name="rawString">Chuỗi thô đúng thứ tự field theo tài liệu MoMo.</param>
        /// <param name="secretKey">Secret key MoMo.</param>
        public static string Sign(string rawString, string secretKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey ?? string.Empty));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawString));
            var builder = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Dựng chuỗi thô cho request tạo thanh toán.
        /// LƯU Ý: thứ tự field phải khớp byte-for-byte với tài liệu MoMo,
        /// nếu không chữ ký tính ra sẽ sai và MoMo từ chối request.
        /// </summary>
        public static string BuildCreateRawString(
            string partnerCode, string accessKey, string requestId, long amount,
            string orderId, string orderInfo, string redirectUrl, string ipnUrl,
            string requestType)
        {
            return $"partnerCode={partnerCode}&accessKey={accessKey}&requestId={requestId}"
                + $"&amount={amount}&orderId={orderId}&orderInfo={orderInfo}"
                + $"&redirectUrl={redirectUrl}&ipnUrl={ipnUrl}&requestType={requestType}";
        }

        /// <summary>
        /// Dựng chuỗi thô để xác thực callback IPN thật của MoMo.
        /// LƯU Ý: MoMo yêu cầu field sắp xếp theo thứ tự alphabet;
        /// phía mình phải dựng y hệt thì chữ ký HMAC mới khớp.
        /// </summary>
        public static string BuildCallbackRawString(
            string accessKey, long amount, string extraData, string message,
            string orderId, string orderInfo, string orderType, string partnerCode,
            string payType, string requestId, long responseTime, int resultCode,
            string transId)
        {
            return $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}"
                + $"&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}"
                + $"&payType={payType}&requestId={requestId}&responseTime={responseTime}"
                + $"&resultCode={resultCode}&transId={transId}";
        }
    }
}
