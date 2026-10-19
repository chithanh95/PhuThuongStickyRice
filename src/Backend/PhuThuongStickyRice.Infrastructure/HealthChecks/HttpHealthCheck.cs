using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.HealthChecks
{
    /// <summary>
    /// Health check kiểu HTTP: ping một URL (GET) và báo Healthy khi
    /// status code 2xx. Dùng để giám sát các endpoint phụ thuộc bên ngoài
    /// (vd: API thanh toán, dịch vụ gửi tin) từ trang /health của hệ thống.
    /// </summary>
    public class HttpHealthCheck : IHealthCheck
    {
        // HttpClient dùng chung cho mọi lần check để tránh socket exhaustion
        // (tạo HttpClient mới mỗi lần ping sẽ cạn kiệt port TCP).
        // SocketsHttpHandler với PooledConnectionLifetime: connection được tái
        // tạo định kỳ nên DNS thay đổi vẫn được cập nhật (HttpClient static giữ
        // connection cũ vô thời hạn sẽ trỏ vào IP cũ). Timeout riêng để một
        // endpoint bị treo không giữ trang /health quá lâu.
        private static readonly HttpClient _httpClient = new(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        private readonly string _uri;

        /// <summary>Khởi tạo health check cho URL cần ping.</summary>
        /// <param name="uri">URL đầy đủ sẽ được GET khi check.</param>
        public HttpHealthCheck(string uri)
        {
            _uri = uri;
        }

        /// <summary>
        /// Ping URL và trả về Healthy khi status code thành công (2xx);
        /// ngược lại trả về FailureStatus đã đăng ký (Unhealthy/Degraded)
        /// kèm status code hoặc thông điệp exception để dễ chẩn đoán.
        /// </summary>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync(_uri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy($"Uri: '{_uri}', StatusCode: '{response.StatusCode}'");
                }
                else
                {
                    return new HealthCheckResult(context.Registration.FailureStatus, $"Uri: '{_uri}', StatusCode: '{response.StatusCode}'");
                }
            }
            catch (Exception exception)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, $"Uri: '{_uri}', Exception: '{exception.Message}'", exception);
            }
        }
    }
}
