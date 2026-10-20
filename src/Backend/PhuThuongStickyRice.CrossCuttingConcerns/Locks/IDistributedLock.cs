using System;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Locks
{
    /// <summary>
    /// Khóa phân tán: đảm bảo chỉ một tiến trình/máy chiếm giữ một tài nguyên
    /// logic cùng lúc, dùng cho các thao tác không được chạy song song
    /// (ví dụ: worker gửi báo cáo tuần, job dọn dẹp). Scope trả về phải
    /// được dispose (dùng <c>using</c>) để giải phóng khóa.
    /// </summary>
    public interface IDistributedLock : IDisposable
    {
        /// <summary>
        /// Giành khóa theo tên, CHỜ (blocking) cho tới khi khóa được giải phóng
        /// rồi trả scope sở hữu. Dùng cho tác vụ nền bắt buộc phải chạy, chỉ
        /// được phép 1 instance tại một thời điểm.
        /// </summary>
        /// <param name="lockName">Tên định danh tài nguyên cần khóa.</param>
        /// <returns>Scope sở hữu khóa; dispose để nhả khóa.</returns>
        IDistributedLockScope Acquire(string lockName);

        /// <summary>
        /// Thử giành khóa mà KHÔNG CHỜ: có khóa ngay thì trả scope, đang bị
        /// giữ thì trả null. Dùng cho tác vụ định kỳ có thể bỏ qua 1 kỳ
        /// (ví dụ: tick đồng bộ cache) thay vì dồn hàng đợi.
        /// </summary>
        /// <param name="lockName">Tên định danh tài nguyên cần khóa.</param>
        /// <returns>Scope sở hữu khóa, hoặc null nếu khóa đang bị giữ.</returns>
        IDistributedLockScope TryAcquire(string lockName);
    }
}
