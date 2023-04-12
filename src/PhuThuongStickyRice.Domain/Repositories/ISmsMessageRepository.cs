using PhuThuongStickyRice.Domain.Entities;
using System;

namespace PhuThuongStickyRice.Domain.Repositories
{
    public interface ISmsMessageRepository : IRepository<SmsMessage, Guid>
    {
    }
}
