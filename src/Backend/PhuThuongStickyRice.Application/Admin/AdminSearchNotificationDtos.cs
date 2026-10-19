using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Admin
{
    /// <summary>
    /// Item 157: one hit of the admin global search (topbar).
    /// Type is "order" | "product" | "customer".
    /// </summary>
    public class GlobalSearchItemDto
    {
        public string Type { get; set; }

        public string Title { get; set; }

        public string Subtitle { get; set; }

        /// <summary>Admin route to jump to (e.g. /orders?search=PX-123).</summary>
        public string Link { get; set; }
    }

    /// <summary>Item 157: grouped global search result.</summary>
    public class GlobalSearchResultDto
    {
        public List<GlobalSearchItemDto> Orders { get; set; } = new List<GlobalSearchItemDto>();

        public List<GlobalSearchItemDto> Products { get; set; } = new List<GlobalSearchItemDto>();

        public List<GlobalSearchItemDto> Customers { get; set; } = new List<GlobalSearchItemDto>();
    }

    /// <summary>
    /// Item 158: one notification for the admin notification center.
    /// Kind is "urgent" | "warning" | "info". Id is stable across reloads
    /// (e.g. "pending-orders", "review-{id}") so the client can remember
    /// read state in localStorage.
    /// </summary>
    public class NotificationDto
    {
        public string Id { get; set; }

        public string Kind { get; set; }

        public string Title { get; set; }

        public string Detail { get; set; }

        public string Link { get; set; }
    }
}
