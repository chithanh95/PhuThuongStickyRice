using PhuThuongStickyRice.CrossCuttingConcerns.OS;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    /// <summary>
    /// Repository truy vấn người dùng: dựng query User theo tùy chọn include
    /// (password history, token, claim, role) phục vụ các màn admin quản trị.
    /// </summary>
    public class UserRepository : Repository<User, Guid>, IUserRepository
    {
        public UserRepository(StickyRiceDbContext dbContext, IDateTimeProvider dateTimeProvider)
            : base(dbContext, dateTimeProvider)
        {
        }

        /// <summary>
        /// Dựng query User theo tùy chọn include/no-tracking (chưa thực thi —
        /// caller gọi ToListAsync/CountAsync để chạy query).
        /// </summary>
        /// <param name="queryOptions">Các navigation cần include và cờ AsNoTracking.</param>
        /// <returns>Query User đã áp dụng các tùy chọn.</returns>
        public IQueryable<User> Get(UserQueryOptions queryOptions)
        {
            var query = GetAll();

            if (queryOptions.IncludePasswordHistories)
            {
                query = query.Include(x => x.PasswordHistories);
            }

            if (queryOptions.IncludeTokens)
            {
                query = query.Include(x => x.Tokens);
            }

            if (queryOptions.IncludeClaims)
            {
                query = query.Include(x => x.Claims);
            }

            if (queryOptions.IncludeUserRoles)
            {
                query = query.Include(x => x.UserRoles);
            }

            if (queryOptions.IncludeRoles)
            {
                query = query.Include("UserRoles.Role");
            }

            if (queryOptions.AsNoTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }
    }
}
