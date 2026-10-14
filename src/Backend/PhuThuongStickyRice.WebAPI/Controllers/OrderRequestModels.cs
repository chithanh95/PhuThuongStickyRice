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

        /// <summary>Optional VAT invoice tax code.</summary>
        public string TaxCode { get; set; }

        /// <summary>Optional VAT invoice company name.</summary>
        public string CompanyName { get; set; }

        /// <summary>Recipient name when ordering for someone else.</summary>
        public string RecipientName { get; set; }

        /// <summary>Recipient phone when ordering for someone else.</summary>
        public string RecipientPhone { get; set; }

        /// <summary>Gift packaging requested.</summary>
        public bool IsGift { get; set; }

        public string GiftMessage { get; set; }

        /// <summary>Loyalty points the customer wants to redeem.</summary>
        public int RedeemPoints { get; set; }

        /// <summary>Referrer's referral code, if any.</summary>
        public string ReferralCode { get; set; }

        /// <summary>Optional birthday (updates the loyalty profile).</summary>
        public DateTime? DateOfBirth { get; set; }

        public List<CreateOrderLineRequestModel> Lines { get; set; } = new List<CreateOrderLineRequestModel>();
    }

    public sealed class CancelOrderRequestModel
    {
        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }
    }

    public sealed class UpdateOrderLineRequestModel
    {
        public Guid LineId { get; set; }

        public int Quantity { get; set; }
    }

    public sealed class UpdateOrderLinesRequestModel
    {
        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        public List<UpdateOrderLineRequestModel> Lines { get; set; } = new List<UpdateOrderLineRequestModel>();
    }

    public sealed class SubmitReviewRequestModel
    {
        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }
    }

    public sealed class SpinRequestModel
    {
        public Guid OrderId { get; set; }

        public string CustomerPhone { get; set; }
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
