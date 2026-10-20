using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Một dòng hàng trong đơn mới: món, số lượng, size, topping, combo và
    /// ghi chú món (in trên nhãn dán).
    /// </summary>
    public sealed class CreateOrderLineRequestModel
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        public Guid? SizeOptionId { get; set; }

        public List<Guid> ToppingIds { get; set; } = [];

        public bool IsCombo { get; set; }

        /// <summary>Optional per-item note (rendered on sticky labels).</summary>
        public string Note { get; set; }
    }

    /// <summary>
    /// Body tạo đơn hàng mới ở checkout: thông tin khách, hình thức thanh
    /// toán/nhận hàng, mã giảm giá, điểm loyalty muốn đổi, mã giới thiệu,
    /// khóa chống trùng đơn (IdempotencyKey), token reCAPTCHA và nguồn đơn.
    /// </summary>
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

        /// <summary>"ShipperPickup" (default), "Pickup" or legacy "Delivery".</summary>
        public string FulfillmentMethod { get; set; }

        /// <summary>
        /// Name of the customer-booked shipper (Grab/Ahamove/...) for a
        /// ShipperPickup order. Optional.
        /// </summary>
        public string PickupShipperName { get; set; }

        /// <summary>
        /// Phone of the customer-booked shipper for a ShipperPickup order.
        /// Optional.
        /// </summary>
        public string PickupShipperPhone { get; set; }

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

        /// <summary>
        /// Mã OTP xác thực SĐT (6 số, gửi qua POST api/phone-otp/send với
        /// purpose "checkout-spend"). Bắt buộc khi khách CHƯA đăng nhập mà
        /// trả bằng ví hoặc đổi điểm loyalty — tức đang tiêu tiền/điểm của
        /// SĐT khai ở checkout; khách đã đăng nhập hoặc không dùng ví/điểm
        /// thì bỏ qua.
        /// </summary>
        public string OtpCode { get; set; }

        /// <summary>Referrer's referral code, if any.</summary>
        public string ReferralCode { get; set; }

        /// <summary>Optional birthday (updates the loyalty profile).</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Chi nhánh khách chọn ở checkout (item 109). Backend chỉ chấp nhận
        /// chi nhánh đang hoạt động và lưu tên chi nhánh lên đơn.
        /// </summary>
        public Guid? BranchId { get; set; }

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

        public List<CreateOrderLineRequestModel> Lines { get; set; } = [];
    }

    /// <summary>
    /// Khách tự hủy đơn ở trang tra cứu: mã đơn + SĐT để xác thực đúng chủ
    /// đơn, kèm lý do hủy bắt buộc (item 233).
    /// </summary>
    public sealed class CancelOrderRequestModel
    {
        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>
        /// Item 233 fix (bug-hunt 2026-10-19): lý do hủy đơn do khách chọn
        /// ở modal tra cứu. Trước đây controller bỏ qua trường này nên
        /// CancelByCustomerAsync luôn nhận null → mọi lần khách tự hủy đều
        /// lỗi "Vui lòng chọn lý do hủy đơn." dù đã chọn lý do.
        /// </summary>
        public string CancelReason { get; set; }
    }

    /// <summary>
    /// Đổi số lượng của một dòng hàng trong đơn (dùng khi khách sửa đơn
    /// chưa xác nhận).
    /// </summary>
    public sealed class UpdateOrderLineRequestModel
    {
        public Guid LineId { get; set; }

        public int Quantity { get; set; }
    }

    /// <summary>
    /// Khách sửa các dòng hàng của đơn đã đặt: mã đơn + SĐT xác thực chủ
    /// đơn + danh sách dòng hàng mới.
    /// </summary>
    public sealed class UpdateOrderLinesRequestModel
    {
        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        public List<UpdateOrderLineRequestModel> Lines { get; set; } = [];
    }

    /// <summary>
    /// Khách đổi địa chỉ giao hàng / khung giờ nhận của đơn đã đặt.
    /// </summary>
    public sealed class UpdateOrderAddressRequestModel
    {
        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerAddress { get; set; }

        public string DeliverySlot { get; set; }
    }

    /// <summary>
    /// Khách đổi SĐT người nhận / ghi chú của đơn đã đặt.
    /// </summary>
    public sealed class UpdateOrderContactRequestModel
    {
        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        public string RecipientPhone { get; set; }

        public string Notes { get; set; }
    }

    /// <summary>
    /// Khách đánh giá đơn hàng: điểm sao, nhận xét, ảnh chụp và điểm sao
    /// chi tiết từng món (Lines).
    /// </summary>
    public sealed class SubmitReviewRequestModel
    {
        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }

        /// <summary>Optional photo URL uploaded via track/review/photo.</summary>
        public string PhotoUrl { get; set; }

        /// <summary>Wave 6b (item 26): optional per-dish star ratings.</summary>
        public List<ReviewLineRequestModel> Lines { get; set; } = [];
    }

    /// <summary>
    /// Điểm sao chi tiết cho một món trong đánh giá đơn hàng.
    /// </summary>
    public sealed class ReviewLineRequestModel
    {
        public Guid ProductId { get; set; }

        public int Rating { get; set; }
    }

    /// <summary>
    /// Lượt quay vòng quay may mắn sau khi đặt đơn: mã đơn + SĐT xác thực
    /// đúng chủ đơn được quay.
    /// </summary>
    public sealed class SpinRequestModel
    {
        public Guid OrderId { get; set; }

        public string CustomerPhone { get; set; }
    }

    /// <summary>
    /// Admin đổi trạng thái đơn hàng (kèm lý do bắt buộc khi hủy đơn).
    /// </summary>
    public sealed class UpdateOrderStatusRequestModel
    {
        public string Status { get; set; }

        /// <summary>Item 233: lý do hủy — nhân viên chọn khi hủy đơn.</summary>
        public string CancelReason { get; set; }
    }

    /// <summary>
    /// Khách báo đã chuyển khoản cho đơn: SĐT để xác thực đúng chủ đơn
    /// trước khi bếp ưu tiên xử lý.
    /// </summary>
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

    /// <summary>
    /// Staff confirms a ShipperPickup order was collected at the shop.
    /// Optionally records the customer-booked shipper info.
    /// </summary>
    public sealed class MarkPickedUpRequestModel
    {
        /// <summary>Shipper name; null/empty clears it.</summary>
        public string PickupShipperName { get; set; }

        /// <summary>Shipper phone; null/empty clears it.</summary>
        public string PickupShipperPhone { get; set; }
    }
}
