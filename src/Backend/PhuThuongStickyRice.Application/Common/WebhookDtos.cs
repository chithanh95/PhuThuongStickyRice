using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Common
{
        public class WebhookEndpointDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }

            /// <summary>Masked secret — never returned to the client.</summary>
            public bool HasSecret { get; set; }
            public string Events { get; set; }
            public bool IsActive { get; set; }
        }

        public class CreateUpdateWebhookEndpointDto
        {
            [Required]
            [StringLength(200)]
            public string Name { get; set; }

            [Required]
            [StringLength(1000)]
            [Url]
            public string Url { get; set; }

            /// <summary>Optional. When empty on update, the existing secret is kept.</summary>
            [StringLength(200)]
            public string Secret { get; set; }

            /// <summary>Comma-separated: OrderCreated, OrderStatusChanged.</summary>
            [StringLength(200)]
            public string Events { get; set; }

            public bool IsActive { get; set; } = true;
        }

        public class WebhookDeliveryDto
        {
            public Guid Id { get; set; }
            public DateTime At { get; set; }
            public Guid EndpointId { get; set; }
            public string EndpointName { get; set; }
            public string Event { get; set; }
            public bool IsSuccess { get; set; }
            public int? StatusCode { get; set; }
            public string Error { get; set; }
        }
}
