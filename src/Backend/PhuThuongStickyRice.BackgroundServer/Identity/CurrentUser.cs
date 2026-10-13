using PhuThuongStickyRice.Domain.Identity;
using System;

namespace PhuThuongStickyRice.BackgroundServer.Identity
{
    public class CurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => false;

        public Guid UserId => Guid.Empty;
    }
}
