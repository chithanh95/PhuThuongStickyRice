using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Orders
{
        public class RequestRefundInput
        {
            [Range(1000, 100000000)]
            public decimal Amount { get; set; }

            [StringLength(500)]
            public string Reason { get; set; }
        }
}
