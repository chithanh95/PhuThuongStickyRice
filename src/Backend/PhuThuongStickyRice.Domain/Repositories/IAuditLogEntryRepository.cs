using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Linq;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// Điều kiện lọc khi truy vấn nhật ký audit (<see cref="AuditLogEntry"/>).
    /// </summary>
    public class AuditLogEntryQueryOptions
    {
        /// <summary>Lọc theo người thực hiện; <see cref="Guid.Empty"/> = không lọc theo người dùng.</summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Lọc theo id đối tượng bị tác động. Dạng chuỗi vì khóa chính
        /// của các entity khác nhau có thể không phải GUID.
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>True khi chỉ đọc dữ liệu: tắt change tracking để query nhanh và nhẹ bộ nhớ hơn.</summary>
        public bool AsNoTracking { get; set; }
    }

    /// <summary>
    /// Repository cho <see cref="AuditLogEntry"/> với truy vấn lọc theo
    /// người dùng / đối tượng bị tác động.
    /// </summary>
    public interface IAuditLogEntryRepository : IRepository<AuditLogEntry, Guid>
    {
        /// <summary>Lấy queryable nhật ký audit theo điều kiện trong <paramref name="queryOptions"/>.</summary>
        /// <param name="queryOptions">Điều kiện lọc (người dùng, đối tượng, chế độ tracking).</param>
        /// <returns>Query nhật ký audit để service tiếp tục lọc/phân trang.</returns>
        IQueryable<AuditLogEntry> Get(AuditLogEntryQueryOptions queryOptions);
    }
}
