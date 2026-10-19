using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Common
{
        /// <summary>
        /// Webhook endpoint đã đăng ký: hệ thống bắn HTTP POST về Url khi sự kiện
        /// đơn hàng (OrderCreated, OrderStatusChanged...) xảy ra. Secret không bao giờ
        /// trả về client — chỉ trả cờ HasSecret để admin biết đã cấu hình hay chưa.
        /// </summary>
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

        /// <summary>
        /// Dữ liệu tạo/sửa webhook endpoint. Khi sửa, để Secret trống thì giữ
        /// nguyên secret cũ (không reset), tránh admin vô tình làm mất secret.
        /// </summary>
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

        /// <summary>
        /// Lịch sử một lần bắn webhook tới endpoint: thời điểm, sự kiện, thành công
        /// hay không, HTTP status và lỗi (nếu có). Dùng để admin debug tích hợp.
        /// </summary>
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
