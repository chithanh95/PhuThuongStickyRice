using System.Collections.Generic;

namespace PhuThuongStickyRice.WebAPI.Authorization
{
    /// <summary>
    /// Tên các authorization policy quản lý sản phẩm. Giá trị hằng số đặt trùng
    /// tên policy để tránh lỗi gõ sai khi dùng string rời rạc ở nhiều chỗ
    /// ([Authorize], đăng ký policy trong Startup, kiểm tra quyền trong JWT).
    /// </summary>
    public static class AuthorizationPolicyNames
    {
        /// <summary>Policy cho phép xem danh sách sản phẩm.</summary>
        public const string GetProductsPolicy = "GetProductsPolicy";
        /// <summary>Policy cho phép xem chi tiết một sản phẩm.</summary>
        public const string GetProductPolicy = "GetProductPolicy";
        /// <summary>Policy cho phép thêm sản phẩm mới.</summary>
        public const string AddProductPolicy = "AddProductPolicy";
        /// <summary>Policy cho phép cập nhật sản phẩm.</summary>
        public const string UpdateProductPolicy = "UpdateProductPolicy";
        /// <summary>Policy cho phép xóa sản phẩm.</summary>
        public const string DeleteProductPolicy = "DeleteProductPolicy";

        /// <summary>
        /// Liệt kê toàn bộ policy — dùng khi đăng ký policy trong
        /// <c>ConfigureServices</c> và khi kiểm tra quyền trong JWT token service.
        /// </summary>
        public static IEnumerable<string> GetPolicyNames()
        {
            yield return GetProductsPolicy;
            yield return GetProductPolicy;
            yield return AddProductPolicy;
            yield return UpdateProductPolicy;
            yield return DeleteProductPolicy;
        }
    }
}
