using PhuThuongStickyRice.CrossCuttingConcerns.OS;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class SmsMessageRepository : Repository<SmsMessage, Guid>, ISmsMessageRepository
    {
        public SmsMessageRepository(StickyRiceDbContext dbContext,
            IDateTimeProvider dateTimeProvider)
            : base(dbContext, dateTimeProvider)
        {
        }
    }
}
