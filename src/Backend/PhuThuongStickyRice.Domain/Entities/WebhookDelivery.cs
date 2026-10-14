using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>Delivery attempt log for outbound webhooks (item 110).</summary>
    public class WebhookDelivery : Entity<Guid>, IAggregateRoot
    {
        public const int EventMaxLength = 100;

        public DateTime At { get; set; }

        public Guid EndpointId { get; set; }

        public string EndpointName { get; set; }

        public string Event { get; set; }

        public bool Success { get; set; }

        public int? StatusCode { get; set; }

        public string Error { get; set; }
    }
}
