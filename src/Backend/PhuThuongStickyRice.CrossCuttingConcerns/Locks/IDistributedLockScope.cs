using System;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Locks
{
    /// <summary>
    /// Phạm vi giữ khóa: tự thả khóa khi Dispose (dùng với <c>using</c>)
    /// để không quên thả khóa khi xử lý xong hoặc gặp exception.
    /// </summary>
    public interface IDistributedLockScope : IDisposable
    {
        /// <summary>
        /// Kiểm tra scope này còn đang giữ khóa không (false khi khóa đã bị
        /// hết hạn hoặc bị chiếm bởi owner khác).
        /// </summary>
        /// <returns>True khi vẫn còn giữ khóa.</returns>
        bool StillHoldingLock();
    }
}
