using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Admin.Models
{
    /// <summary>Item 87: end-of-day inventory count (kiểm kê cuối ngày).</summary>
    public class InventoryLineModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public int RemainingQty { get; set; }

        public string Note { get; set; }
    }

    public class SaveInventoryModel
    {
        public DateTime Date { get; set; }

        public List<InventoryLineModel> Lines { get; set; } = new List<InventoryLineModel>();
    }

    public class InventoryHistoryLineModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public int RemainingQty { get; set; }

        public string Note { get; set; }

        public int? PreviousRemainingQty { get; set; }

        public int? DiffVsPrevious { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class InventoryHistoryDayModel
    {
        public DateTime Date { get; set; }

        public List<InventoryHistoryLineModel> Lines { get; set; } = new List<InventoryHistoryLineModel>();
    }
}
