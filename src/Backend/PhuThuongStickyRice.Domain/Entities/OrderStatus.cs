namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Lifecycle of a customer order.
    /// </summary>
    public enum OrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Preparing = 2,
        Delivering = 3,
        Completed = 4,
        Cancelled = 5
    }
}
