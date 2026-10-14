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

        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        public List<CreateOrderLineModel> Lines { get; set; } = new List<CreateOrderLineModel>();
    }

    public class PlacedOrderModel
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; }
    }
}
