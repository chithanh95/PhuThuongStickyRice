using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    public sealed class CreateOrderLineRequestModel
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }
    }

    public sealed class CreateOrderRequestModel
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        public List<CreateOrderLineRequestModel> Lines { get; set; } = new List<CreateOrderLineRequestModel>();
    }

    public sealed class UpdateOrderStatusRequestModel
    {
        public string Status { get; set; }
    }
}
