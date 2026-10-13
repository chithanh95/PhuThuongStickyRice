using PhuThuongStickyRice.CrossCuttingConcerns.OS;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class EmailMessageRepository : Repository<EmailMessage, Guid>, IEmailMessageRepository
    {
        public EmailMessageRepository(StickyRiceDbContext dbContext,
            IDateTimeProvider dateTimeProvider)
            : base(dbContext, dateTimeProvider)
        {
        }
    }
}
