using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Linq;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// Tùy chọn truy vấn người dùng: chỉ định các quan hệ cần nạp kèm
    /// (eager-load) và chế độ tracking của EF Core.
    /// </summary>
    public class UserQueryOptions
    {
        /// <summary>Nạp kèm lịch sử mật khẩu (dùng khi kiểm tra trùng mật khẩu cũ lúc đổi pass).</summary>
        public bool IncludePasswordHistories { get; set; }
        /// <summary>Nạp kèm các claim của người dùng.</summary>
        public bool IncludeClaims { get; set; }
        /// <summary>Nạp kèm bảng trung gian UserRole.</summary>
        public bool IncludeUserRoles { get; set; }
        /// <summary>Nạp kèm Role (thông qua UserRoles).</summary>
        public bool IncludeRoles { get; set; }
        /// <summary>Nạp kèm các token của người dùng (vd refresh token).</summary>
        public bool IncludeTokens { get; set; }
        /// <summary>True khi chỉ đọc dữ liệu: tắt change tracking để query nhanh và nhẹ bộ nhớ hơn.</summary>
        public bool AsNoTracking { get; set; }
    }

    /// <summary>
    /// Repository cho <see cref="User"/>: mở rộng <see cref="IRepository{TEntity,TKey}"/>
    /// chuẩn với truy vấn hỗ trợ eager-load quan hệ.
    /// </summary>
    public interface IUserRepository : IRepository<User, Guid>
    {
        /// <summary>
        /// Lấy queryable người dùng kèm các quan hệ được chỉ định trong
        /// <paramref name="queryOptions"/> (dùng Include tương ứng).
        /// </summary>
        /// <param name="queryOptions">Các quan hệ cần nạp kèm và chế độ tracking.</param>
        /// <returns>Query người dùng để service tiếp tục lọc/phân trang.</returns>
        IQueryable<User> Get(UserQueryOptions queryOptions);
    }
}
