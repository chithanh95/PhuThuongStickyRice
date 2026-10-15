using System;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    /// <summary>
    /// One ticket on the public waiting-number screen (/cho, item 178).
    /// Privacy: exposes only the queue number, a short code and the
    /// coarse status — no phone, no name, no amounts.
    /// </summary>
    public class QueueTicketDto
    {
        /// <summary>Sequential number of the order within the Vietnam day (1, 2, 3...).</summary>
        public int QueueNumber { get; set; }

        /// <summary>Last 3 characters of the order number, e.g. "821".</summary>
        public string ShortCode { get; set; }

        /// <summary>"Preparing" (đang chuẩn bị) or "Ready" (mời lấy món).</summary>
        public string Status { get; set; }
    }
}
