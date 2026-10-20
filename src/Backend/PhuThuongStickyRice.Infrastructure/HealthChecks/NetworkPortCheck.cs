using System;
using System.Net.Sockets;
using System.Threading;

namespace PhuThuongStickyRice.Infrastructure.HealthChecks
{
    /// <summary>
    /// Kiểm tra cổng TCP (host:port) có chấp nhận kết nối không. Dùng làm
    /// startup gate — vd: Migrator chờ SQL Server sẵn sàng trước khi migrate.
    /// </summary>
    public class NetworkPortCheck
    {
        /// <summary>
        /// Trả true khi cổng TCP chấp nhận kết nối; false khi timeout/kết nối thất bại.
        /// </summary>
        /// <param name="address">Địa chỉ dạng "host:port" (vd: "localhost,1433" phải viết "localhost:1433").</param>
        public static bool IsReady(string address)
        {
            // Fail-fast với message rõ ràng thay vì IndexOutOfRangeException khó hiểu
            // khi config sai định dạng (dùng duy nhất ở Migrator startup gate).
            var parts = address.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException(
                    $"Địa chỉ kiểm tra port không hợp lệ: '{address}'. Định dạng đúng là 'host:port'.",
                    nameof(address));
            }

            string host = parts[0];
            // TryParse thay vì Parse: config sai port (vd "localhost:abc") thì
            // báo lỗi rõ ràng ngay, thay vì FormatException khó hiểu.
            if (!int.TryParse(parts[1], out int port))
            {
                throw new ArgumentException(
                    $"Port kiểm tra không hợp lệ: '{parts[1]}'. Định dạng đúng là 'host:port' (vd 'localhost:1433').",
                    nameof(address));
            }

            try
            {
                using var client = new TcpClient(host, port);
                return client.Connected;
            }
            catch (Exception ex)
            {
                // Log đầy đủ exception (type + stack trace), không chỉ Message —
                // khi port check thất bại ở Migrator startup gate, stack trace
                // giúp phân biệt lỗi DNS/config với lỗi mạng thật.
                Console.WriteLine($"Failed to connect to {address}: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Chờ cổng TCP sẵn sàng, thử lại tối đa <paramref name="retries"/> lần.
        /// </summary>
        /// <param name="address">Địa chỉ dạng "host:port".</param>
        /// <param name="retries">Số lần thử lại sau khi lần đầu thất bại (0 = chỉ thử 1 lần).</param>
        /// <param name="timeout">Khoảng nghỉ giữa các lần thử (ms).</param>
        public static void Wait(string address, int retries = 0, int timeout = 30000)
        {
            int count = 0;
            do
            {
                var isReady = IsReady(address);
                if (isReady)
                {
                    return;
                }

                count++;
                Thread.Sleep(timeout);
            }
            while (count <= retries);
        }
    }
}
