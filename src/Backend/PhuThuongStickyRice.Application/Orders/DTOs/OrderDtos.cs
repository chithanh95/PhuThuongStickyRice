using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    public class OrderLineDto
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }
    }

    public class OrderDto : EntityDto<Guid>
    {
        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        public string Status { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; }

        /// <summary>Lines subtotal before discount and fee.</summary>
        public decimal Subtotal { get; set; }

        public decimal DeliveryFee { get; set; }

        public string DiscountCode { get; set; }

        public decimal DiscountAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<OrderLineDto> Lines { get; set; } = new List<OrderLineDto>();
    }

    public class CreateOrderLineDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class CreateOrderDto
    {
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(30)]
        public string CustomerPhone { get; set; }

        /// <summary>
        /// Optional. When provided, the order confirmation email is sent here.
        /// </summary>
        [EmailAddress]
        [StringLength(254)]
        public string CustomerEmail { get; set; }

        [Required]
        [StringLength(500)]
        public string CustomerAddress { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        /// <summary>Payment method: "Cod" (default), "BankTransfer" or "MoMo".</summary>
        public string PaymentMethod { get; set; }

        /// <summary>Optional discount/promo code entered at checkout.</summary>
        [StringLength(50)]
        public string DiscountCode { get; set; }

        public List<CreateOrderLineDto> Lines { get; set; } = new List<CreateOrderLineDto>();
    }

    /// <summary>Customer aggregated by phone number (admin).</summary>
    public class CustomerDto
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public long OrderCount { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime LastOrderAt { get; set; }
    }

    public class GetOrdersInput : PagedAndSortedResultRequestDto
    {
        public string SearchTerm { get; set; }

        public string Status { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        [Required]
        public string Status { get; set; }
    }

    /// <summary>Customer marks their bank transfer as sent. Phone must match the order.</summary>
    public class MarkTransferredDto
    {
        [Required]
        public string CustomerPhone { get; set; }
    }

    /// <summary>Public order lookup: order number + phone (the phone proves ownership).</summary>
    public class TrackOrderInput
    {
        [Required]
        public string OrderNumber { get; set; }

        [Required]
        public string CustomerPhone { get; set; }
    }

    /// <summary>Public bank-transfer details shown at checkout (account info + VietQR).</summary>
    public class BankTransferInfoDto
    {
        public string BankName { get; set; }

        public string BankId { get; set; }

        public string AccountNumber { get; set; }

        public string AccountName { get; set; }

        public string QrTemplate { get; set; }
    }

    /// <summary>Response of POST /api/orders/{id}/momo/create.</summary>
    public class MoMoCreatePaymentResultDto
    {
        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; }

        public decimal Amount { get; set; }

        /// <summary>
        /// Where the buyer is sent to pay. In TestMode this is the in-app
        /// MoMo simulator page; in production it is the real MoMo payUrl.
        /// </summary>
        public string PayUrl { get; set; }

        public bool TestMode { get; set; }
    }

    /// <summary>
    /// MoMo return / IPN payload. In TestMode the simulator posts the echoed
    /// create-signature (<see cref="SimulatedSignature"/>) plus the result the
    /// buyer picked; in production MoMo posts its signed IPN fields.
    /// </summary>
    public class MoMoCallbackDto
    {
        public string OrderId { get; set; }

        public string RequestId { get; set; }

        public long Amount { get; set; }

        public string OrderInfo { get; set; }

        public int ResultCode { get; set; }

        public string Message { get; set; }

        public string TransId { get; set; }

        public string PayType { get; set; }

        public long ResponseTime { get; set; }

        public string ExtraData { get; set; }

        public string Signature { get; set; }

        /// <summary>Echoed create-signature, only used by the TEST simulator.</summary>
        public string SimulatedSignature { get; set; }

        public bool Simulated { get; set; }
    }

    public class DashboardStatsDto
    {
        public long TotalOrders { get; set; }

        public long PendingOrders { get; set; }

        public decimal TotalRevenue { get; set; }

        public long TotalProductsSold { get; set; }

        public long TotalProducts { get; set; }

        public List<OrdersByStatusDto> OrdersByStatus { get; set; } = new List<OrdersByStatusDto>();

        public List<RevenueByDayDto> RevenueByDay { get; set; } = new List<RevenueByDayDto>();

        public List<OrderDto> RecentOrders { get; set; } = new List<OrderDto>();
    }

    public class OrdersByStatusDto
    {
        public string Status { get; set; }

        public long Count { get; set; }
    }

    public class RevenueByDayDto
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }

        public long Orders { get; set; }
    }

    public class TopProductDto
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public long QuantitySold { get; set; }

        public decimal Revenue { get; set; }
    }
}
