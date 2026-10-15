namespace PhuThuongStickyRice.Web.Components.Shop.Models
{
    /// <summary>Item 178: one ticket on the waiting-number TV screen (/cho).</summary>
    public class QueueTicketModel
    {
        public int QueueNumber { get; set; }
        public string ShortCode { get; set; }

        /// <summary>"Preparing" (đang chuẩn bị) or "Ready" (mời lấy món).</summary>
        public string Status { get; set; }

        public bool IsReady => Status == "Ready";
    }
}
