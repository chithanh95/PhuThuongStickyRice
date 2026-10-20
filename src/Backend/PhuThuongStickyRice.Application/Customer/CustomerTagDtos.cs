using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Customer
{
    /// <summary>
    /// Tag gắn cho khách hàng (do admin gắn tay ở màn hình khách hàng):
    /// phân loại nhanh như VIP, khách sỉ, nợ... để nhân viên chú ý khi xử lý đơn.
    /// </summary>
    public class CustomerTagDto
    {
        /// <summary>Id tag.</summary>
        public Guid Id { get; set; }

        /// <summary>SĐT khách hàng sở hữu tag.</summary>
        public string CustomerPhone { get; set; }

        /// <summary>Tên tag (vd: VIP, Khách sỉ).</summary>
        public string TagName { get; set; }

        /// <summary>Màu hex hiển thị tag trên admin (vd: #e74c3c).</summary>
        public string Color { get; set; }

        /// <summary>Người tạo tag (tên đăng nhập nhân viên).</summary>
        public string CreatedBy { get; set; }

        /// <summary>Thời điểm tạo tag (UTC).</summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Thông tin tạo tag mới cho khách hàng.</summary>
    public class CreateCustomerTagDto
    {
        /// <summary>SĐT khách hàng cần gắn tag.</summary>
        public string CustomerPhone { get; set; }

        /// <summary>Tên tag (có thể chọn từ danh sách gợi ý <see cref="CustomerTagPresets"/>).</summary>
        public string TagName { get; set; }

        /// <summary>Màu hex tự chọn; bỏ trống thì dùng màu gợi ý theo tên tag.</summary>
        public string Color { get; set; }
    }

    /// <summary>
    /// Tên tag gợi ý hiển thị trong datalist của admin, kèm màu mặc định
    /// cho từng tên — để nhân viên chọn nhanh mà không cần nhập màu tay.
    /// </summary>
    public static class CustomerTagPresets
    {
        /// <summary>Danh sách cặp (tên tag, màu hex) gợi ý cho admin.</summary>
        public static readonly IReadOnlyList<(string Name, string Color)> All =
            new List<(string, string)>
            {
                ("VIP", "#e74c3c"),
                ("Khách sỉ", "#8e44ad"),
                ("Khách quen", "#27ae60"),
                ("Cần ưu tiên", "#f39c12"),
                ("Nợ", "#e67e22"),
                ("Khó tính", "#7f8c8d"),
            };

        /// <summary>Màu xám trung tính dùng khi tag không khớp tên gợi ý nào.</summary>
        public const string DefaultColor = "#6c757d";

        /// <summary>
        /// Tra màu hiển thị cho một tên tag: khớp tên gợi ý (không phân biệt
        /// hoa thường, bỏ khoảng trắng thừa) thì trả màu của nó, ngược lại
        /// trả <see cref="DefaultColor"/> — tag tự nhập vẫn có màu hợp lý.
        /// </summary>
        /// <param name="tagName">Tên tag cần tra màu.</param>
        /// <returns>Mã màu hex.</returns>
        public static string ColorFor(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return DefaultColor;
            }

            foreach (var (name, color) in All)
            {
                if (string.Equals(name, tagName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return color;
                }
            }

            return DefaultColor;
        }
    }
}
