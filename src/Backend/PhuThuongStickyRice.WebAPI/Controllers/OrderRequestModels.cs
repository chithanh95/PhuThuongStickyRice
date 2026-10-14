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

        /// <summary>Optional per-item note (rendered on sticky labels).</summary>
        public string Note { get; set; }
    }

        public sealed class CreateOrderRequestModel
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        /// <summary>"Cod" (default), "BankTransfer", "MoMo" or "Wallet".</summary>
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

        /// <summary>
        /// Idempotency key for the checkout attempt. Retrying with the same
        /// key within 24h returns the existing order.
        /// </summary>
        public string IdempotencyKey { get; set; }

        /// <summary>reCAPTCHA v3 token (verified when reCAPTCHA is enabled).</summary>
        public string RecaptchaToken { get; set; }

        /// <summary>
        /// Order source (item 133): "Web", "QR bàn", "Link chia sẻ", "Nhóm".
        /// Set by the storefront from shop entry params.
        /// </summary>
        public string Source { get; set; }

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

        /// <summary>Wave 6b (item 26): optional per-dish star ratings.</summary>
        public List<ReviewLineRequestModel> Lines { get; set; } = new List<ReviewLineRequestModel>();
    }

    public sealed class ReviewLineRequestModel
    {
        public Guid ProductId { get; set; }

        public int Rating { get; set; }
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

    /// <summary>Wave 6c (item 33): admin edits the internal order note.</summary>
    public sealed class UpdateAdminNoteRequestModel
    {
        /// <summary>Internal note; null/empty clears it.</summary>
        public string Note { get; set; }
    }
}
