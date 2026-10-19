using System;
using System.Net.Sockets;
using System.Threading;

namespace PhuThuongStickyRice.Infrastructure.HealthChecks
{
    public class NetworkPortCheck
    {
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
                Console.WriteLine($"Failed to connect to {address}: {ex.Message}");
                return false;
            }
        }

        public static void Wait(string address, int retries = 0, int timeOut = 30000)
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
                Thread.Sleep(timeOut);
            }
            while (count <= retries);
        }
    }
}
