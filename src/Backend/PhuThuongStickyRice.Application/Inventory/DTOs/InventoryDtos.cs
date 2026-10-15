using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Inventory.DTOs
{
    public class InventoryLineInput
    {
        public Guid ProductId { get; set; }

        public int RemainingQty { get; set; }

        public string Note { get; set; }
    }

    public class SaveDailyInventoryInput
    {
        /// <summary>Business day counted (date part is used).</summary>
        public DateTime Date { get; set; }

        public List<InventoryLineInput> Lines { get; set; } = new List<InventoryLineInput>();
    }

    public class DailyInventoryLineDto
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public int RemainingQty { get; set; }

        public string Note { get; set; }

        /// <summary>
        /// Remaining qty at the most recent earlier count of the same
        /// product, if any — used to show the day-over-day difference.
        /// </summary>
        public int? PreviousRemainingQty { get; set; }

        public int? DiffVsPrevious => PreviousRemainingQty.HasValue
            ? RemainingQty - PreviousRemainingQty.Value
            : (int?)null;

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class DailyInventoryDayDto
    {
        public DateTime Date { get; set; }

        public List<DailyInventoryLineDto> Lines { get; set; } = new List<DailyInventoryLineDto>();
    }
}
