using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Permissions
{
    public class PermissionDefinitionDto
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Group { get; set; }
    }

    public class RolePermissionsDto
    {
        public string Role { get; set; }
        public string RoleLabel { get; set; }
        public List<string> GrantedKeys { get; set; } = new List<string>();
    }

    public class PermissionMatrixDto
    {
        public List<PermissionDefinitionDto> Definitions { get; set; } = new List<PermissionDefinitionDto>();
        public List<RolePermissionsDto> Roles { get; set; } = new List<RolePermissionsDto>();
    }

    public class UpdateRolePermissionsDto
    {
        public string Role { get; set; }
        public List<string> GrantedKeys { get; set; } = new List<string>();
    }

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
