using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Customer
{
    public class CustomerTagDto
    {
        public Guid Id { get; set; }

        public string CustomerPhone { get; set; }

        public string TagName { get; set; }

        public string Color { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateCustomerTagDto
    {
        public string CustomerPhone { get; set; }

        public string TagName { get; set; }

        /// <summary>Optional hex color; a preset color is used when empty.</summary>
        public string Color { get; set; }
    }

    /// <summary>Suggested tag names shown in the admin UI datalist.</summary>
    public static class CustomerTagPresets
    {
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

        public const string DefaultColor = "#6c757d";

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
