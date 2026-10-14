using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Outbound webhook endpoint. On <c>OrderCreated</c> /
    /// <c>OrderStatusChanged</c> the app POSTs JSON to <see cref="Url"/>
    /// with an HMAC-SHA256 signature header (<c>X-Webhook-Signature</c>).
    /// </summary>
    public class WebhookEndpoint : Entity<Guid>, IAggregateRoot
    {
        public const string EventOrderCreated = "OrderCreated";
        public const string EventOrderStatusChanged = "OrderStatusChanged";

        public const int NameMaxLength = 200;
        public const int UrlMaxLength = 1000;
        public const int SecretMaxLength = 200;
        public const int EventsMaxLength = 200;

        public string Name { get; set; }

        public string Url { get; set; }

        /// <summary>HMAC-SHA256 signing secret. Kept server-side only.</summary>
        public string Secret { get; set; }

        /// <summary>Comma-separated event names, e.g. "OrderCreated,OrderStatusChanged".</summary>
        public string Events { get; set; }

        public bool IsActive { get; set; }
    }
}
