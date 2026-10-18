using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Reports
{
        public class NewReturningCustomersDto
        {
            public long NewCustomers { get; set; }

            public long ReturningCustomers { get; set; }

            public long NewCustomerOrders { get; set; }

            public long ReturningCustomerOrders { get; set; }
        }

        public class OwnerReportResultDto
        {
            public string Period { get; set; }

            public bool Sent { get; set; }

            public string Reason { get; set; }
        }
}
