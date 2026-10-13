using PhuThuongStickyRice.Domain.Entities;
using System;

namespace PhuThuongStickyRice.Domain.Repositories
{
    public interface IEmailMessageRepository : IRepository<EmailMessage, Guid>
    {
    }
}
