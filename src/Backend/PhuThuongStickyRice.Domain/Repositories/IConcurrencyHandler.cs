using System;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// Xử lý xung đột đồng thời (optimistic concurrency) trên entity:
    /// gán row-version đọc từ DB và nhận diện lỗi xung đột của EF Core —
    /// tránh mất cập nhật khi 2 admin sửa cùng một bản ghi.
    /// </summary>
    /// <typeparam name="TEntity">Loại entity cần bảo vệ đồng thời.</typeparam>
    public interface IConcurrencyHandler<TEntity>
    {
        /// <summary>
        /// Gán giá trị row-version hiện tại cho entity trước khi lưu —
        /// EF Core dùng nó để phát hiện xung đột (WHERE ... AND RowVersion = ...).
        /// </summary>
        /// <param name="entity">Entity cần gán.</param>
        /// <param name="version">Giá trị row-version mới nhất đọc từ DB.</param>
        void SetRowVersion(TEntity entity, byte[] version);

        /// <summary>
        /// Kiểm tra một exception có phải lỗi xung đột đồng thời của DB
        /// (DbUpdateConcurrencyException) hay không.
        /// </summary>
        /// <param name="ex">Exception cần kiểm tra.</param>
        /// <returns>True nếu là lỗi xung đột đồng thời.</returns>
        bool IsDbUpdateConcurrencyException(Exception ex);
    }
}
