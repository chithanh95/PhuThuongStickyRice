using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Permissions
{
    /// <summary>Định nghĩa một quyền trong hệ thống: mã quyền (vd "orders.refund"), tên hiển thị và nhóm.</summary>
    public class PermissionDefinitionDto
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Group { get; set; }
    }

    /// <summary>Quyền của một vai trò: tên vai trò, tên hiển thị và danh sách mã quyền được cấp.</summary>
    public class RolePermissionsDto
    {
        public string Role { get; set; }
        public string RoleLabel { get; set; }
        public List<string> GrantedKeys { get; set; } = new List<string>();
    }

    /// <summary>Ma trận phân quyền đầy đủ: toàn bộ định nghĩa quyền và quyền từng vai trò — vẽ ở màn hình admin.</summary>
    public class PermissionMatrixDto
    {
        public List<PermissionDefinitionDto> Definitions { get; set; } = new List<PermissionDefinitionDto>();
        public List<RolePermissionsDto> Roles { get; set; } = new List<RolePermissionsDto>();
    }

    /// <summary>Input admin gán quyền cho vai trò: tên vai trò và danh sách mã quyền được cấp.</summary>
    public class UpdateRolePermissionsDto
    {
        public string Role { get; set; }
        public List<string> GrantedKeys { get; set; } = new List<string>();
    }

    /// <summary>Nhãn tiếng Việt hiển thị tên vai trò ở màn hình phân quyền (Admin/Thu ngân/Bếp).</summary>
    public static class RoleLabels
    {
        public static string Get(string role) => role switch
        {
            "Admin" => "Quản trị (Admin)",
            "Cashier" => "Thu ngân (Cashier)",
            "Kitchen" => "Bếp (Kitchen)",
            _ => role,
        };
    }
}
