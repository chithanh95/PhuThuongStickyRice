using PhuThuongStickyRice.Domain.Identity;
using System;

namespace PhuThuongStickyRice.Infrastructure.Identity
{
    public class AnonymousUser : ICurrentUser
    {
        public bool IsAuthenticated => false;

        public Guid UserId => Guid.Empty;
    }
}
