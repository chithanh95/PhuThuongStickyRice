using PhuThuongStickyRice.CrossCuttingConcerns.CircuitBreakers;
using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Persistence.CircuitBreakers
{
    /// <summary>
    /// Entity lưu trạng thái circuit breaker (chống gọi dồn vào dịch vụ ngoài
    /// đang lỗi, ví dụ SMS/Zalo gateway): khi trạng thái Open, mọi lời gọi
    /// bị chặn ngay tại <see cref="EnsureOkStatus"/> để dịch vụ lỗi có thời
    /// gian hồi phục, tránh snowball.
    /// </summary>
    public class CircuitBreaker : ICircuitBreaker
    {
        /// <summary>Khóa chính.</summary>
        public Guid Id { get; set; }

        /// <summary>Tên định danh mạch (ví dụ "SmsGateway"), duy nhất trong hệ thống.</summary>
        public string Name { get; set; }

        /// <summary>Trạng thái hiện tại: Closed (thông suốt) hoặc Open (đang chặn).</summary>
        public CircuitStatus Status { get; set; }

        /// <summary>Thời điểm tạo bản ghi (UTC offset).</summary>
        public DateTimeOffset CreatedDateTime { get; set; }

        /// <summary>Thời điểm trạng thái thay đổi lần cuối — dùng để tính thời gian hồi phục.</summary>
        public DateTimeOffset LastStatusUpdated { get; set; }

        /// <summary>Lịch sử các lần đổi trạng thái của mạch.</summary>
        public ICollection<CircuitBreakerLog> CircuitBreakerLogs { get; set; }

        /// <summary>
        /// Chặn cứng nếu mạch đang Open: ném <see cref="CircuitBreakerOpenException"/>
        /// để caller không gọi vào dịch vụ đang lỗi. Gọi ở đầu mỗi lần dùng
        /// dịch vụ ngoài được bảo vệ bởi mạch.
        /// </summary>
        public void EnsureOkStatus()
        {
            if (Status == CircuitStatus.Open)
            {
                throw new CircuitBreakerOpenException();
            }
        }
    }
}
