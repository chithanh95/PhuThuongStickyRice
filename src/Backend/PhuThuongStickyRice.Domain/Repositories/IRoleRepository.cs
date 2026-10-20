using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Linq;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// Tùy chọn truy vấn vai trò: chỉ định các quan hệ cần nạp kèm
    /// (eager-load) và chế độ tracking của EF Core.
    /// </summary>
    public class RoleQueryOptions
    {
        /// <summary>Nạp kèm các claim của vai trò.</summary>
        public bool IncludeClaims { get; set; }
        /// <summary>Nạp kèm bảng trung gian UserRole.</summary>
        public bool IncludeUserRoles { get; set; }
        /// <summary>Nạp kèm người dùng thuộc vai trò (thông qua UserRoles).</summary>
        public bool IncludeUsers { get; set; }
        /// <summary>True khi chỉ đọc dữ liệu: tắt change tracking để query nhanh và nhẹ bộ nhớ hơn.</summary>
        public bool AsNoTracking { get; set; }
    }

    /// <summary>
    /// Repository cho <see cref="Role"/>: mở rộng <see cref="IRepository{TEntity,TKey}"/>
    /// chuẩn với truy vấn hỗ trợ eager-load quan hệ.
    /// </summary>
    public interface IRoleRepository : IRepository<Role, Guid>
    {
        /// <summary>
        /// Lấy queryable vai trò kèm các quan hệ được chỉ định trong
        /// <paramref name="queryOptions"/> (dùng Include tương ứng).
        /// </summary>
        /// <param name="queryOptions">Các quan hệ cần nạp kèm và chế độ tracking.</param>
        /// <returns>Query vai trò để service tiếp tục lọc/phân trang.</returns>
        IQueryable<Role> Get(RoleQueryOptions queryOptions);
    }
}
