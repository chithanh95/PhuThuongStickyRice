using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Permissions
{
    /// <summary>
    /// Item 155: fine-grained permission keys. Each key maps to real
    /// sensitive buttons/endpoints in the admin. Admin implicitly holds
    /// every key; other roles hold what /admin/phan-quyen grants them.
    /// </summary>
    public static class PermissionKeys
    {
        public const string OrdersConfirm = "orders.confirm";
        public const string OrdersCancel = "orders.cancel";
        public const string OrdersConfirmPayment = "orders.confirm-payment";
        public const string OrdersRefund = "orders.refund";
        public const string ProductsEdit = "products.edit";
        public const string ProductsDelete = "products.delete";
        public const string DiscountsManage = "discounts.manage";
        public const string ReportsView = "reports.view";
        public const string SettingsEdit = "settings.edit";
        public const string StaffManage = "staff.manage";

        /// <summary>
        /// Định nghĩa 1 quyền chi tiết dùng trên ma trận phân quyền
        /// (/admin/phan-quyen): key dùng để kiểm tra ở endpoint/nút, label +
        /// group hiển thị cho admin khi gán quyền cho vai trò.
        /// </summary>
        public sealed class PermissionDefinition
        {
            /// <summary>Khởi tạo định nghĩa quyền.</summary>
            /// <param name="key">Mã quyền (vd "orders.confirm") dùng để kiểm tra.</param>
            /// <param name="label">Tên hiển thị trên ma trận phân quyền.</param>
            /// <param name="group">Nhóm quyền (Đơn hàng, Sản phẩm...) để gộp hiển thị.</param>
            public PermissionDefinition(string key, string label, string group)
            {
                Key = key;
                Label = label;
                Group = group;
            }

            /// <summary>Mã quyền duy nhất, khớp với const trong <see cref="PermissionKeys"/>.</summary>
            public string Key { get; }
            /// <summary>Tên hiển thị của quyền trên giao diện phân quyền.</summary>
            public string Label { get; }
            /// <summary>Nhóm gom quyền khi render ma trận phân quyền.</summary>
            public string Group { get; }
        }

        public static readonly IReadOnlyList<PermissionDefinition> All =
            new List<PermissionDefinition>
            {
                new PermissionDefinition(OrdersConfirm, "Xác nhận / chuyển trạng thái đơn", "Đơn hàng"),
                new PermissionDefinition(OrdersCancel, "Hủy đơn", "Đơn hàng"),
                new PermissionDefinition(OrdersConfirmPayment, "Xác nhận \"Đã nhận tiền\"", "Đơn hàng"),
                new PermissionDefinition(OrdersRefund, "Hoàn tiền đơn", "Đơn hàng"),
                new PermissionDefinition(ProductsEdit, "Thêm / sửa / bật-tắt món", "Sản phẩm"),
                new PermissionDefinition(ProductsDelete, "Xóa món", "Sản phẩm"),
                new PermissionDefinition(DiscountsManage, "Quản lý mã giảm giá", "Bán hàng"),
                new PermissionDefinition(ReportsView, "Xem báo cáo / dự báo doanh thu", "Tài chính"),
                new PermissionDefinition(SettingsEdit, "Sửa cài đặt cửa hàng", "Hệ thống"),
                new PermissionDefinition(StaffManage, "Quản lý nhân viên & phân quyền", "Hệ thống"),
            };

        /// <summary>
        /// Roles shown on the /admin/phan-quyen matrix. Admin is not
        /// listed — it always holds every permission.
        /// </summary>
        public static readonly IReadOnlyList<string> MatrixRoles =
            new List<string> { "Cashier", "Kitchen" };

        /// <summary>
        /// Safe defaults seeded at startup. Cashier handles orders and cash
        /// but cannot cancel/refund or touch products, discounts, reports,
        /// settings or staff. Kitchen only uses the kitchen screen.
        /// </summary>
        public static IReadOnlyDictionary<string, string[]> DefaultGrants { get; } =
            new Dictionary<string, string[]>
            {
                ["Admin"] = new[]
                {
                    OrdersConfirm, OrdersCancel, OrdersConfirmPayment, OrdersRefund,
                    ProductsEdit, ProductsDelete, DiscountsManage, ReportsView,
                    SettingsEdit, StaffManage,
                },
                ["Cashier"] = new[]
                {
                    OrdersConfirm, OrdersConfirmPayment,
                },
                ["Kitchen"] = System.Array.Empty<string>(),
            };
    }
}
