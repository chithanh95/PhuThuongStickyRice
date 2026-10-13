using System;

namespace PhuThuongStickyRice.Domain.Identity
{
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }

        Guid UserId { get; }
    }
}
