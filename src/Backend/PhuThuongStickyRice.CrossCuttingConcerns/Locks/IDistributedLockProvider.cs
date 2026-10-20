using System;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Locks
{
    /// <summary>
    /// Khóa phân tán (distributed lock) lưu trên SQL Server: đảm bảo nhiều
    /// instance/server không xử lý trùng cùng một bản ghi (vd trừ điểm
    /// loyalty, chốt đơn, sinh mã tuần tự) cùng một lúc.
    /// </summary>
    public interface IDistributedLockProvider
    {
        /// <summary>
        /// Xin giữ khóa trên entity; trả về false khi đã có chủ khác giữ.
        /// Khóa tự hết hạn sau <paramref name="expiration"/> để tránh kẹt
        /// khóa nếu tiến trình giữ khóa bị crash mà không kịp thả.
        /// </summary>
        /// <param name="entityName">Tên loại thực thể cần khóa.</param>
        /// <param name="entityId">Id bản ghi cần khóa.</param>
        /// <param name="ownerId">Id của tiến trình/instance xin khóa.</param>
        /// <param name="expiration">Thời gian giữ khóa tối đa.</param>
        /// <returns>True khi chiếm được khóa.</returns>
        bool AcquireLock(string entityName, string entityId, string ownerId, TimeSpan expiration);

        /// <summary>
        /// Gia hạn thêm <paramref name="expiration"/> cho khóa đang giữ;
        /// trả về false khi khóa đã mất (bị chiếm hoặc đã hết hạn).
        /// </summary>
        /// <param name="entityName">Tên loại thực thể.</param>
        /// <param name="entityId">Id bản ghi.</param>
        /// <param name="ownerId">Id chủ sở hữu khóa.</param>
        /// <param name="expiration">Thời gian gia hạn thêm.</param>
        /// <returns>True khi còn giữ khóa sau khi gia hạn.</returns>
        bool ExtendLock(string entityName, string entityId, string ownerId, TimeSpan expiration);

        /// <summary>
        /// Thả khóa đang giữ. Chỉ chủ sở hữu <paramref name="ownerId"/> mới
        /// thả được để tránh instance khác thả nhầm khóa của mình.
        /// </summary>
        /// <param name="entityName">Tên loại thực thể.</param>
        /// <param name="entityId">Id bản ghi.</param>
        /// <param name="ownerId">Id chủ sở hữu khóa.</param>
        /// <returns>True khi thả thành công.</returns>
        bool ReleaseLock(string entityName, string entityId, string ownerId);

        /// <summary>
        /// Thả toàn bộ khóa mà <paramref name="ownerId"/> đang giữ —
        /// dùng khi một instance dừng/hủy tác vụ dở dang.
        /// </summary>
        /// <param name="ownerId">Id chủ sở hữu các khóa cần thả.</param>
        /// <returns>True khi thả thành công.</returns>
        bool ReleaseLocks(string ownerId);

        /// <summary>
        /// Thả các khóa đã quá hạn mà chủ không kịp gia hạn — dọn dẹp định kỳ
        /// để không còn khóa "ma" chiếm tài nguyên.
        /// </summary>
        /// <returns>True khi dọn dẹp thành công.</returns>
        bool ReleaseExpiredLocks();

        /// <summary>
        /// Như <see cref="AcquireLock"/> nhưng xin không được thì ném
        /// <see cref="CouldNotAcquireLockException"/> thay vì trả về false —
        /// dùng cho flow bắt buộc loại trừ lẫn nhau.
        /// </summary>
        /// <param name="entityName">Tên loại thực thể.</param>
        /// <param name="entityId">Id bản ghi.</param>
        /// <param name="ownerId">Id của tiến trình/instance xin khóa.</param>
        /// <param name="expiration">Thời gian giữ khóa tối đa.</param>
        void EnsureAcquiringLock(string entityName, string entityId, string ownerId, TimeSpan expiration);
    }
}
