namespace PhuThuongStickyRice.Application.Permissions
{
    /// <summary>
    /// Nhóm role dùng trong <c>[Authorize(Roles = ...)]</c> — gom chuỗi
    /// "Admin,Cashier" từng gõ tay rải rác ở nhiều service về 1 const để sửa
    /// 1 chỗ khi thay đổi phân quyền và tránh gõ sai chuỗi ở từng attribute.
    /// Dùng <c>const</c> (không phải static readonly) để gắn được vào attribute.
    /// </summary>
    public static class RoleGroups
    {
        /// <summary>Nhân sự được vào các endpoint đơn hàng/POS: Admin và Cashier.</summary>
        public const string AdminCashier = "Admin,Cashier";
    }
}
