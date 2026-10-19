using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// Đơn vị công việc (unit of work) của một thao tác ghi DB: gom nhiều
    /// thay đổi repository thành 1 lần lưu duy nhất, đảm bảo hoặc thành
    /// công toàn bộ hoặc rollback (ví dụ tạo đơn + trừ ví + tăng lượt mã
    /// trong cùng 1 UoW).
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>Lưu mọi thay đổi đang chờ trong unit of work này xuống DB.</summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>Bắt đầu transaction DB với isolation level chỉ định.</summary>
        Task<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bắt đầu transaction DB có đặt tên lock (chống 2 tiến trình cùng
        /// chạy 1 thao tác nhạy cảm như chốt đơn/ghi sổ).
        /// </summary>
        Task<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, string lockName = null, CancellationToken cancellationToken = default);

        /// <summary>Commit transaction đang mở của unit of work này.</summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    }
}
