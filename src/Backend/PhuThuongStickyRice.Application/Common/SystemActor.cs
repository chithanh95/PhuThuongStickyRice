namespace PhuThuongStickyRice.Application.Common
{
    // [codequality-smell 2026-10-20] Gom chuỗi "system" bị hardcode ở
    // WalletAppService / ProductAppService / ShiftCloseAppService về 1 hằng
    // số duy nhất — giá trị này phải giống nhau ở mọi nơi vì là quy ước
    // hiển thị/log cho thao tác chạy tự động.
    /// <summary>
    /// Giá trị ghi vào các trường người thực hiện (CreatedBy/changedBy/...) khi
    /// thao tác chạy tự động (worker, webhook, hệ thống) mà không có nhân viên
    /// đăng nhập. Giữ thống nhất để log/lịch sử lọc được.
    /// </summary>
    public static class SystemActor
    {
        /// <summary>Chuỗi định danh thao tác tự động của hệ thống.</summary>
        public const string Value = "system";
    }
}
