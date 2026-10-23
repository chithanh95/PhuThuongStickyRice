using Microsoft.AspNetCore.Identity;

namespace PhuThuongStickyRice.Infrastructure.Identity
{
    /// <summary>
    /// Mô tả lỗi Identity bằng tiếng Việt. <see cref="IdentityErrorDescriber"/>
    /// mặc định của ASP.NET Core trả toàn tiếng Anh ("Passwords must be at
    /// least 6 characters.", "Username '...' is already taken."...) — các
    /// chuỗi này được AuthController nối lại trả thẳng về form đăng ký /
    /// đổi mật khẩu / tạo tài khoản nhân viên nên khách Việt thấy lỗi lạ
    /// (user báo 2026-10-23). Code của từng lỗi giữ nguyên (nameof) để code
    /// khác đối chiếu theo Code không bị ảnh hưởng — chỉ dịch Description.
    /// </summary>
    public class VietnameseIdentityErrorDescriber : IdentityErrorDescriber
    {
        /// <summary>Lỗi mặc định không xác định.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError DefaultError()
            => new() { Code = nameof(DefaultError), Description = "Đã xảy ra lỗi. Vui lòng thử lại." };

        /// <summary>Lỗi xung đột dữ liệu khi 2 thao tác sửa cùng lúc.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError ConcurrencyFailure()
            => new() { Code = nameof(ConcurrencyFailure), Description = "Thông tin vừa bị thay đổi ở nơi khác. Vui lòng tải lại trang rồi thử lại." };

        /// <summary>Mật khẩu nhập lại không khớp.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError PasswordMismatch()
            => new() { Code = nameof(PasswordMismatch), Description = "Mật khẩu không đúng." };

        /// <summary>Token (đặt lại mật khẩu/xác thực) không hợp lệ hoặc hết hạn.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError InvalidToken()
            => new() { Code = nameof(InvalidToken), Description = "Mã xác thực không hợp lệ hoặc đã hết hạn." };

        /// <summary>Dùng lại mã khôi phục đã dùng.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError RecoveryCodeRedemptionFailed()
            => new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "Mã khôi phục không đúng. Vui lòng thử lại." };

        /// <summary>Tài khoản ngoài (Google/Zalo...) đã gắn với tài khoản khác.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError LoginAlreadyAssociated()
            => new() { Code = nameof(LoginAlreadyAssociated), Description = "Tài khoản đăng nhập này đã được gắn với một tài khoản khác." };

        /// <summary>Tên đăng nhập chứa ký tự không hợp lệ.</summary>
        /// <param name="userName">Tên đăng nhập bị từ chối.</param>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError InvalidUserName(string userName)
            => new() { Code = nameof(InvalidUserName), Description = $"Tên đăng nhập '{userName}' không hợp lệ — chỉ dùng chữ cái và chữ số." };

        /// <summary>Tên vai trò không hợp lệ.</summary>
        /// <param name="role">Tên vai trò bị từ chối.</param>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError InvalidRoleName(string role)
            => new() { Code = nameof(InvalidRoleName), Description = $"Tên vai trò '{role}' không hợp lệ." };

        /// <summary>Email sai định dạng.</summary>
        /// <param name="email">Email bị từ chối.</param>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError InvalidEmail(string email)
            => new() { Code = nameof(InvalidEmail), Description = $"Email '{email}' không đúng định dạng." };

        /// <summary>Email đã có tài khoản khác dùng.</summary>
        /// <param name="email">Email bị trùng.</param>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError DuplicateEmail(string email)
            => new() { Code = nameof(DuplicateEmail), Description = $"Email '{email}' đã được tài khoản khác sử dụng." };

        /// <summary>Tên đăng nhập/SĐT đã có tài khoản khác dùng.</summary>
        /// <param name="userName">Tên đăng nhập bị trùng.</param>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError DuplicateUserName(string userName)
            => new() { Code = nameof(DuplicateUserName), Description = $"'{userName}' đã được đăng ký. Vui lòng đăng nhập." };

        /// <summary>Vai trò đã tồn tại.</summary>
        /// <param name="role">Tên vai trò bị trùng.</param>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError DuplicateRoleName(string role)
            => new() { Code = nameof(DuplicateRoleName), Description = $"Vai trò '{role}' đã tồn tại." };

        /// <summary>Tài khoản đã có mật khẩu.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError UserAlreadyHasPassword()
            => new() { Code = nameof(UserAlreadyHasPassword), Description = "Tài khoản đã có mật khẩu." };

        /// <summary>Người dùng đã thuộc vai trò này.</summary>
        /// <param name="role">Tên vai trò.</param>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError UserAlreadyInRole(string role)
            => new() { Code = nameof(UserAlreadyInRole), Description = $"Tài khoản đã có vai trò '{role}'." };

        /// <summary>Người dùng chưa thuộc vai trò này.</summary>
        /// <param name="role">Tên vai trò.</param>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError UserNotInRole(string role)
            => new() { Code = nameof(UserNotInRole), Description = $"Tài khoản chưa có vai trò '{role}'." };

        /// <summary>Mật khẩu ngắn hơn độ dài tối thiểu.</summary>
        /// <param name="length">Độ dài tối thiểu yêu cầu.</param>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError PasswordTooShort(int length)
            => new() { Code = nameof(PasswordTooShort), Description = $"Mật khẩu phải có ít nhất {length} ký tự." };

        /// <summary>Mật khẩu cần ít nhất 1 ký tự đặc biệt.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError PasswordRequiresNonAlphanumeric()
            => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (không phải chữ/số)." };

        /// <summary>Mật khẩu cần ít nhất 1 chữ số.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError PasswordRequiresDigit()
            => new() { Code = nameof(PasswordRequiresDigit), Description = "Mật khẩu phải chứa ít nhất 1 chữ số (0-9)." };

        /// <summary>Mật khẩu cần ít nhất 1 chữ thường.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError PasswordRequiresLower()
            => new() { Code = nameof(PasswordRequiresLower), Description = "Mật khẩu phải chứa ít nhất 1 chữ thường (a-z)." };

        /// <summary>Mật khẩu cần ít nhất 1 chữ hoa.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError PasswordRequiresUpper()
            => new() { Code = nameof(PasswordRequiresUpper), Description = "Mật khẩu phải chứa ít nhất 1 chữ hoa (A-Z)." };

        /// <summary>Mật khẩu cần nhiều ký tự khác nhau hơn.</summary>
        /// <param name="uniqueChars">Số ký tự khác nhau tối thiểu.</param>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
            => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Mật khẩu phải chứa ít nhất {uniqueChars} ký tự khác nhau." };

        /// <summary>Tài khoản chưa bật tính năng khóa đăng nhập.</summary>
        /// <returns>Lỗi tiếng Việt tương ứng.</returns>
        public override IdentityError UserLockoutNotEnabled()
            => new() { Code = nameof(UserLockoutNotEnabled), Description = "Tài khoản chưa bật tính năng khóa đăng nhập." };
    }
}
