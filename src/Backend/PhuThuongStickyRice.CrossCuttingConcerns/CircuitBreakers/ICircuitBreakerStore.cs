using System;

namespace PhuThuongStickyRice.CrossCuttingConcerns.CircuitBreakers
{
    /// <summary>
    /// Kho lưu trữ trạng thái circuit breaker (mạch ngắt) của các dịch vụ ngoài.
    /// Ghi nhận thành công/thất bại của từng cuộc gọi để tự động "mở mạch"
    /// khi dịch vụ ngoài gặp sự cố, tránh gọi dồn gây treo hệ thống.
    /// </summary>
    public interface ICircuitBreakerStore
    {
        ICircuitBreaker GetCircuitBreaker(string name, TimeSpan openTime);

        void LogSuccess(ICircuitBreaker circuitBreaker);

        void LogFailure(ICircuitBreaker circuitBreaker, int maximumNumberOfFailures, TimeSpan period);
    }
}
