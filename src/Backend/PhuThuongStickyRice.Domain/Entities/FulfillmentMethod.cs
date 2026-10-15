namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// How the customer receives the order. The shop does not deliver:
    /// either the customer's own shipper (Grab/Ahamove/...) picks up at
    /// the shop, or the customer picks up in person.
    /// </summary>
    public enum FulfillmentMethod
    {
        /// <summary>Legacy: shop home delivery (no longer offered).</summary>
        Delivery = 0,

        /// <summary>Customer picks up in person at the shop.</summary>
        Pickup = 1,

        /// <summary>
        /// Customer-booked shipper picks up at the shop. The shop prepares
        /// the order; the shipper reads the order number to collect it.
        /// </summary>
        ShipperPickup = 2
    }
}
