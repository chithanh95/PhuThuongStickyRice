namespace PhuThuongStickyRice.Application.Staff
{
    /// <summary>
    /// Item 156: machine keys for <see cref="Domain.Entities.StaffActivityLog"/> rows.
    /// </summary>
    public static class StaffActivityActions
    {
        public const string Login = "staff.login";
        public const string OrderConfirm = "order.confirm";
        public const string OrderCancel = "order.cancel";
        public const string OrderStatusChange = "order.status";
        public const string OrderConfirmPayment = "order.confirm-payment";
        public const string OrderPickedUp = "order.picked-up";
        public const string OrderRefundRequest = "order.refund-request";
        public const string OrderRefundConfirm = "order.refund-confirm";
        public const string ProductCreate = "product.create";
        public const string ProductUpdate = "product.update";
        public const string ProductDelete = "product.delete";
        public const string DiscountCreate = "discount.create";
        public const string DiscountUpdate = "discount.update";
        public const string DiscountDelete = "discount.delete";
        public const string SettingsUpdate = "settings.update";
        public const string BirthdayGift = "customer.birthday-gift";
        public const string PermissionChange = "staff.permission-change";
        public const string StaffCreate = "staff.create";
        public const string StaffRoleChange = "staff.role-change";

        public static string Label(string action) => action switch
        {
            Login => "Đăng nhập",
            OrderConfirm => "Xác nhận đơn",
            OrderCancel => "Hủy đơn",
            OrderStatusChange => "Chuyển trạng thái đơn",
            OrderConfirmPayment => "Xác nhận đã nhận tiền",
            OrderPickedUp => "Xác nhận đã lấy hàng",
            OrderRefundRequest => "Yêu cầu hoàn tiền",
            OrderRefundConfirm => "Xác nhận hoàn tiền",
            ProductCreate => "Thêm món",
            ProductUpdate => "Sửa món",
            ProductDelete => "Xóa món",
            DiscountCreate => "Thêm mã giảm giá",
            DiscountUpdate => "Sửa mã giảm giá",
            DiscountDelete => "Xóa mã giảm giá",
            SettingsUpdate => "Sửa cài đặt cửa hàng",
            BirthdayGift => "Tặng quà sinh nhật",
            PermissionChange => "Đổi phân quyền",
            StaffCreate => "Tạo tài khoản nhân viên",
            StaffRoleChange => "Đổi vai trò nhân viên",
            _ => action,
        };
    }
}
