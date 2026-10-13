using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Domain.Identity
{
    public interface IPasswordHasher
    {
        bool VerifyHashedPassword(User user, string hashedPassword, string providedPassword);
    }
}
