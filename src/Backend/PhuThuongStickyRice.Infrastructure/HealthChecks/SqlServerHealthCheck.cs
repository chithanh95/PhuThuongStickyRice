using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.HealthChecks
{
    /// <summary>
    /// Health check cho SQL Server: mở kết nối và chạy một câu truy vấn probe
    /// nhẹ (vd: "SELECT 1"). Báo Healthy khi query chạy thành công, ngược lại
    /// trả về FailureStatus đã đăng ký kèm exception để dễ chẩn đoán.
    /// </summary>
    public class SqlServerHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        private readonly string _healthCheckSql;

        /// <summary>Khởi tạo health check SQL Server.</summary>
        /// <param name="connectionString">Chuỗi kết nối tới database cần kiểm tra.</param>
        /// <param name="healthCheckSql">Câu truy vấn probe nhẹ (vd: "SELECT 1").</param>
        public SqlServerHealthCheck(string connectionString, string healthCheckSql)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _healthCheckSql = healthCheckSql ?? throw new ArgumentNullException(nameof(healthCheckSql));
        }

        /// <summary>
        /// Mở kết nối SQL Server và chạy câu probe; trả về Healthy khi thành công.
        /// </summary>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // FIX: SqlConnection/SqlCommand implement IAsyncDisposable — trong method
                // async dùng "await using" để DisposeAsync giải phóng connection bất đồng bộ,
                // tránh block thread pool khi server còn giữ handle.
                await using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = _healthCheckSql;
                await command.ExecuteScalarAsync(cancellationToken);

                return HealthCheckResult.Healthy();
            }
            catch (Exception exception)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, null, exception);
            }
        }
    }
}
