using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    public sealed class CreateOrderLineRequestModel
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        public Guid? SizeOptionId { get; set; }

        public List<Guid> ToppingIds { get; set; } = new List<Guid>();

        public bool IsCombo { get; set; }
    }

        public sealed class CreateOrderRequestModel
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        /// <summary>"Cod" (default), "BankTransfer" or "MoMo".</summary>
        public string PaymentMethod { get; set; }

        /// <summary>Optional discount/promo code.</summary>
        public string DiscountCode { get; set; }

        /// <summary>"Delivery" (default) or "Pickup".</summary>
        public string FulfillmentMethod { get; set; }

        public List<CreateOrderLineRequestModel> Lines { get; set; } = new List<CreateOrderLineRequestModel>();
    }

    public sealed class UpdateOrderStatusRequestModel
    {
        public string Status { get; set; }
    }

    public sealed class MarkTransferredRequestModel
    {
        public string CustomerPhone { get; set; }
    }
}
