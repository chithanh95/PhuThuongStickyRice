namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Vòng đời của một đơn hàng đặt xôi.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>Đơn mới tạo, đang chờ admin/khách xác nhận.</summary>
        Pending = 0,

        /// <summary>Đã xác nhận, bếp bắt đầu nhận đơn.</summary>
        Confirmed = 1,

        /// <summary>Đang nấu/hấp — hiện trên màn hình bếp (KDS).</summary>
        Preparing = 2,

        /// <summary>Đã giao cho shipper/khách đang trên đường nhận.</summary>
        Delivering = 3,

        /// <summary>Đơn hoàn thành — điểm loyalty và doanh thu được chốt ở trạng thái này.</summary>
        Completed = 4,

        /// <summary>Đơn đã hủy (khách tự hủy hoặc admin hủy, kèm lý do).</summary>
        Cancelled = 5
    }
}
