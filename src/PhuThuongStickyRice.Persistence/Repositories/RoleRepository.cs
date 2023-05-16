using Microsoft.EntityFrameworkCore;
using PhuThuongStickyRice.CrossCuttingConcerns.OS;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Linq;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class RoleRepository : Repository<Role, Guid>, IRoleRepository
    {
        public RoleRepository(StickyRiceDbContext dbContext, IDateTimeProvider dateTimeProvider)
            : base(dbContext, dateTimeProvider)
        {
        }

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
                query = query = query.AsNoTracking();
            }

            return query;
        }
    }
}
