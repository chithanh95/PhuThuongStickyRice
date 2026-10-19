using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// Repository chung cho aggregate root của domain: truy vấn LINQ
    /// (IQueryable, chưa thực thi — caller quyết định filter rồi mới
    /// materialize), thêm/sửa/xóa và bulk operation. Mọi thay đổi đi qua
    /// <see cref="UnitOfWork"/> chung để lưu nguyên tử.
    /// </summary>
    /// <typeparam name="TEntity">Aggregate root.</typeparam>
    /// <typeparam name="TKey">Kiểu khóa chính.</typeparam>
    public interface IRepository<TEntity, TKey> : IConcurrencyHandler<TEntity>
        where TEntity : Entity<TKey>, IAggregateRoot
    {
        /// <summary>Unit of work mà repository này ghi vào.</summary>
        IUnitOfWork UnitOfWork { get; }

        /// <summary>Query gốc (chưa thực thi) để caller gắn filter/phân trang.</summary>
        IQueryable<TEntity> GetAll();

        /// <summary>Thêm mới nếu chưa có, cập nhật nếu đã tồn tại.</summary>
        Task AddOrUpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Thêm entity mới. Tự sinh <see cref="Guid"/> khi khóa là Guid rỗng
        /// (xem fix ở Persistence Repository) để không trùng khóa chính.
        /// </summary>
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Cập nhật entity đã tồn tại.</summary>
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Xóa entity (xóa mềm nếu entity hỗ trợ, ngược lại xóa cứng).</summary>
        void Delete(TEntity entity);

        /// <summary>Thực thi query, trả bản ghi đầu tiên hoặc default khi không có.</summary>
        Task<T> FirstOrDefaultAsync<T>(IQueryable<T> query);

        /// <summary>Thực thi query, mong đợi tối đa 1 bản ghi (ném khi có nhiều hơn 1).</summary>
        Task<T> SingleOrDefaultAsync<T>(IQueryable<T> query);

        /// <summary>Thực thi query và nạp toàn bộ kết quả vào bộ nhớ.</summary>
        Task<List<T>> ToListAsync<T>(IQueryable<T> query);

        /// <summary>Đếm số bản ghi của query (chạy COUNT tại DB, không nạp dữ liệu).</summary>
        Task<long> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

        /// <summary>Chèn hàng loạt 1 câu lệnh — dùng cho seed/import số lượng lớn, nhanh hơn AddAsync từng dòng.</summary>
        void BulkInsert(IEnumerable<TEntity> entities);

        /// <summary>Chèn hàng loạt, chỉ các cột được chỉ định.</summary>
        void BulkInsert(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> columnNamesSelector);

        /// <summary>Cập nhật hàng loạt, chỉ các cột được chỉ định.</summary>
        void BulkUpdate(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> columnNamesSelector);

        /// <summary>Upsert hàng loạt: khớp theo cột id, update các cột chỉ định, insert các cột chỉ định khi chưa có.</summary>
        void BulkMerge(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> idSelector, Expression<Func<TEntity, object>> updateColumnNamesSelector, Expression<Func<TEntity, object>> insertColumnNamesSelector);

        /// <summary>Xóa cứng hàng loạt 1 câu lệnh.</summary>
        void BulkDelete(IEnumerable<TEntity> entities);
    }
}
