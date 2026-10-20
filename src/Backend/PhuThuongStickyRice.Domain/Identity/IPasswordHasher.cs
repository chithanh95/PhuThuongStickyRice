using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Domain.Identity
{
    /// <summary>
    /// Abstraction xác minh mật khẩu người dùng trong Domain. Domain chỉ giữ
    /// interface; triển khai cụ thể (BCrypt/PBKDF2…) nằm ở tầng ngoài nên có
    /// thể đổi thuật toán băm mà không sửa logic nghiệp vụ. Tương thích chữ ký
    /// của <c>IPasswordHasher</c> trong ASP.NET Identity để tái sử dụng provider sẵn có.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Kiểm tra mật khẩu do người dùng cung cấp có khớp với hash đã lưu hay không.
        /// </summary>
        /// <param name="user">Người dùng cần xác minh (truyền vào để provider có thể gắn salt hoặc context theo user khi cần).</param>
        /// <param name="hashedPassword">Chuỗi hash mật khẩu đã lưu trong database.</param>
        /// <param name="providedPassword">Mật khẩu thô do người dùng nhập.</param>
        /// <returns><c>true</c> nếu mật khẩu khớp, ngược lại <c>false</c>.</returns>
        bool VerifyHashedPassword(User user, string hashedPassword, string providedPassword);
    }
}
