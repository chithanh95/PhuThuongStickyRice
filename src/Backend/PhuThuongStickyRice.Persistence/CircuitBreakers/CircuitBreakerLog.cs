using PhuThuongStickyRice.CrossCuttingConcerns.CircuitBreakers;
using System;

namespace PhuThuongStickyRice.Persistence.CircuitBreakers
{
    /// <summary>
    /// Bản ghi nhật ký mỗi lần circuit breaker đổi trạng thái: để truy vết
    /// vì sao mạch mở/đóng (thất bại liên tiếp hay đã hồi phục thành công)
    /// khi chẩn đoán sự cố dịch vụ ngoài.
    /// </summary>
    public class CircuitBreakerLog
    {
        /// <summary>Khóa chính.</summary>
        public Guid Id { get; set; }

        /// <summary>Id của mạch mà bản ghi này thuộc về.</summary>
        public Guid CircuitBreakerId { get; set; }

        /// <summary>Trạng thái mới sau lần đổi (Closed/Open).</summary>
        public CircuitStatus Status { get; set; }

        /// <summary>True = lần kiểm tra thành công dẫn tới đổi trạng thái (mạch đóng lại); false = thất bại dẫn tới mở mạch.</summary>
        public bool Succeeded { get; set; }

        /// <summary>Thời điểm ghi log (UTC offset).</summary>
        public DateTimeOffset CreatedDateTime { get; set; }
    }
}
