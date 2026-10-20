using PhuThuongStickyRice.CrossCuttingConcerns.OS;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    /// <summary>
    /// Repository truy vấn vai trò (role): dựng query Role theo tùy chọn include
    /// (claim, user) phục vụ các màn admin phân quyền.
    /// </summary>
    public class RoleRepository : Repository<Role, Guid>, IRoleRepository
    {
        public RoleRepository(StickyRiceDbContext dbContext, IDateTimeProvider dateTimeProvider)
            : base(dbContext, dateTimeProvider)
        {
        }

        /// <summary>
        /// Dựng query Role theo tùy chọn include/no-tracking (chưa thực thi —
        /// caller gọi ToListAsync/CountAsync để chạy query).
        /// </summary>
        /// <param name="queryOptions">Các navigation cần include và cờ AsNoTracking.</param>
        /// <returns>Query Role đã áp dụng các tùy chọn.</returns>
        public IQueryable<Role> Get(RoleQueryOptions queryOptions)
        {
            var query = GetAll();

            if (queryOptions.IncludeClaims)
            {
                query = query.Include(x => x.Claims);
            }

            if (queryOptions.IncludeUserRoles)
            {
                query = query.Include(x => x.UserRoles);
            }

            if (queryOptions.IncludeUsers)
            {
                query = query.Include("UserRoles.User");
            }

            if (queryOptions.AsNoTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }
    }
}
