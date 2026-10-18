using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Orders
{
        public class MoMoOrderDto
        {
            public Guid Id { get; set; }
            public string OrderNumber { get; set; }
            public string CustomerName { get; set; }
            public string CustomerPhone { get; set; }
            public decimal TotalAmount { get; set; }
            public string PaymentStatus { get; set; }
            public bool MoMoMatched { get; set; }
            public DateTime CreatedAt { get; set; }
        }
}
