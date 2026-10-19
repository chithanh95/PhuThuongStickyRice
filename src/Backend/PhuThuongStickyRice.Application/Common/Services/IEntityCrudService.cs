using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Service CRUD chung cho các entity đơn giản (danh mục, cài đặt...),
    /// tránh viết lặp đi lặp lại logic thêm/sửa/xóa cơ bản.
    /// Các entity có nghiệp vụ phức tạp (đơn hàng, thanh toán) dùng
    /// AppService riêng thay vì interface này.
    /// </summary>
    /// <typeparam name="T">Entity (aggregate root, khóa Guid).</typeparam>
    public interface IEntityCrudService<T>
        where T : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Lấy toàn bộ danh sách entity.</summary>
        /// <param name="cancellationToken">Token hủy thao tác.</param>
        /// <returns>Danh sách entity.</returns>
        Task<List<T>> GetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy một entity theo Id.
        /// </summary>
        /// <param name="id">Id của entity.</param>
        /// <param name="cancellationToken">Token hủy thao tác.</param>
        /// <returns>Entity tìm thấy; ném NotFoundException nếu không có.</returns>
        Task<T> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Thêm mới nếu chưa tồn tại, cập nhật nếu đã có (upsert).
        /// </summary>
        /// <param name="entity">Entity cần lưu.</param>
        /// <param name="cancellationToken">Token hủy thao tác.</param>
        Task AddOrUpdateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>Thêm mới entity.</summary>
        /// <param name="entity">Entity cần thêm.</param>
        /// <param name="cancellationToken">Token hủy thao tác.</param>
        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>Cập nhật entity đã tồn tại.</summary>
        /// <param name="entity">Entity cần cập nhật.</param>
        /// <param name="cancellationToken">Token hủy thao tác.</param>
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>Xóa entity khỏi hệ thống.</summary>
        /// <param name="entity">Entity cần xóa.</param>
        /// <param name="cancellationToken">Token hủy thao tác.</param>
        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    }
}
