using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Blazor.Modules.Shop.Models
{
    public class PagedResult<T>
    {
        public long TotalCount { get; set; }

        public List<T> Items { get; set; } = new List<T>();
    }

    public class ShopProductModel
    {
        public Guid Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; }

        public string Status { get; set; }
    }

    public class CartItemModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductCode { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class CreateOrderLineModel
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }
    }

    public class CreateOrderModel
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>Optional. When provided, the order confirmation email is sent here.</summary>
        public string CustomerEmail { get; set; }

        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        /// <summary>"Cod" (default), "BankTransfer" or "MoMo".</summary>
        public string PaymentMethod { get; set; } = "Cod";

        public List<CreateOrderLineModel> Lines { get; set; } = new List<CreateOrderLineModel>();
    }

    public class PlacedOrderModel
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerAddress { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }
    }

    /// <summary>Public bank-transfer details shown at checkout (account info + VietQR).</summary>
    public class BankTransferInfoModel
    {
        public string BankName { get; set; }

        public string BankId { get; set; }

        public string AccountNumber { get; set; }

        public string AccountName { get; set; }

        public string QrTemplate { get; set; }

        /// <summary>True when the shop has configured a real bank account (no 'xxx' placeholders).</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(BankId) && BankId != "xxx"
            && !string.IsNullOrWhiteSpace(AccountNumber) && AccountNumber != "xxx";
    }

    public class MoMoCreatePaymentResultModel
    {
        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; }

        public decimal Amount { get; set; }

        public string PayUrl { get; set; }

        public bool TestMode { get; set; }
    }

    public class MoMoCallbackModel
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

        public string SimulatedSignature { get; set; }

        public bool Simulated { get; set; }
    }

    public class TrackedOrderLineModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }
    }

    /// <summary>Order as returned by the public /api/orders/track endpoint.</summary>
    public class TrackedOrderModel
    {
        public Guid Id { get; set; }

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

        public DateTime CreatedAt { get; set; }

        public List<TrackedOrderLineModel> Lines { get; set; } = new List<TrackedOrderLineModel>();
    }
}
