using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Admin.Models
{
    /// <summary>Item 157: one hit of the admin global search.</summary>
    public class GlobalSearchItemModel
    {
        public string Type { get; set; }

        public string Title { get; set; }

        public string Subtitle { get; set; }

        public string Link { get; set; }
    }

    /// <summary>Item 157: grouped global search result.</summary>
    public class GlobalSearchResultModel
    {
        public List<GlobalSearchItemModel> Orders { get; set; } = new List<GlobalSearchItemModel>();

        public List<GlobalSearchItemModel> Products { get; set; } = new List<GlobalSearchItemModel>();

        public List<GlobalSearchItemModel> Customers { get; set; } = new List<GlobalSearchItemModel>();

        public bool HasAny =>
            (Orders?.Count ?? 0) + (Products?.Count ?? 0) + (Customers?.Count ?? 0) > 0;
    }

    /// <summary>Item 158: one notification for the admin notification center.</summary>
    public class NotificationModel
    {
        public string Id { get; set; }

        public string Kind { get; set; }

        public string Title { get; set; }

        public string Detail { get; set; }

        public string Link { get; set; }
    }
}
