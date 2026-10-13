using PhuThuongStickyRice.CrossCuttingConcerns.OS;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class AuditLogEntryRepository : Repository<AuditLogEntry, Guid>, IAuditLogEntryRepository
    {
        public AuditLogEntryRepository(StickyRiceDbContext dbContext, IDateTimeProvider dateTimeProvider)
            : base(dbContext, dateTimeProvider)
        {
        }

        public IQueryable<AuditLogEntry> Get(AuditLogEntryQueryOptions queryOptions)
        {
            var query = GetAll();

            if (queryOptions.UserId != Guid.Empty)
            {
                query = query.Where(x => x.UserId == queryOptions.UserId);
            }

            if (!string.IsNullOrEmpty(queryOptions.ObjectId))
            {
                query = query.Where(x => x.ObjectId == queryOptions.ObjectId);
            }

            if (queryOptions.AsNoTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }
    }
}
