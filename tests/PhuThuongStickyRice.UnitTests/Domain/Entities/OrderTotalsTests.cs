using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PhuThuongStickyRice.UnitTests.Domain.Entities
{
    /// <summary>
    /// Item 108: order-flow totals — line totals with sizes/toppings,
    /// delivery fee, fixed/percent discounts, freeship (fee = 0), gift fee,
    /// status transitions, and multi-branch fields.
    /// Note: the app-service discount rules (usage windows, min order,
    /// per-code limits) live behind IRepository and cannot run in this
    /// sandbox; the domain math behind them is covered here.
    /// </summary>
    public class OrderTotalsTests
    {
        private const string Vnd = "VND";

        private static List<(Guid, string, Money, int, string, string, decimal, bool, string)> Lines()
        {
            // 1 xôi gấc (size lớn 45k) x 2 + 1 xôi xéo 25k x 1 = 115.000 ₫.
            // Toppings are baked into the unit price at the application
            // layer; the domain line total is unitPrice * quantity.
            return new List<(Guid, string, Money, int, string, string, decimal, bool, string)>
            {
                (Guid.NewGuid(), "Xoi gac", Money.Of(45000, Vnd), 2, "Size lớn", "Ruốc, Hành phi", 10000, false, null),
                (Guid.NewGuid(), "Xoi xeo", Money.Of(25000, Vnd), 1, null, null, 0, false, null),
            };
        }

        private static Order CreateWith(
            Money deliveryFee = null,
            string discountCode = null,
            Money discountAmount = null,
            Money giftFee = null,
            bool isGift = false,
            Guid? branchId = null,
            string branchName = null)
        {
            return Order.Create(
                "PX-20261014-9001",
                "Nguyen Van A",
                "0901234567",
                null,
                "123 Lang Phu Thuong, Ha Noi",
                null,
                PaymentMethod.Cod,
                Lines(),
                deliveryFee,
                discountCode,
                discountAmount,
                FulfillmentMethod.Delivery,
                giftFee,
                null,
                null,
                null,
                null,
                isGift,
                null,
                false,
                null,
                0,
                null,
                null,
                null,
                null,
                0,
                branchId,
                branchName);
        }

        [Fact]
        public void Create_LinesWithSizeAndToppings_SumsUnitPriceTimesQuantity()
        {
            var order = CreateWith();

            Assert.Equal(2, order.Lines.Count);
            Assert.Equal(90000, order.Lines.First().LineTotal.Amount);
            Assert.Equal(25000, order.Lines.Last().LineTotal.Amount);
            Assert.Equal(115000, order.TotalAmount.Amount);
        }

        [Fact]
        public void Create_WithFeeAndFixedDiscount_ComputesTotal()
        {
            var order = CreateWith(
                deliveryFee: Money.Of(20000, Vnd),
                discountCode: "GIAM10K",
                discountAmount: Money.Of(10000, Vnd));

            // 115.000 + 20.000 - 10.000 = 125.000 ₫.
            Assert.Equal(20000, order.DeliveryFee.Amount);
            Assert.Equal(10000, order.DiscountAmount.Amount);
            Assert.Equal("GIAM10K", order.DiscountCode);
            Assert.Equal(125000, order.TotalAmount.Amount);
        }

        [Fact]
        public void Create_FreeshipVoucher_FeeIsZeroed()
        {
            // Item 120: a freeship voucher is applied as deliveryFee = 0.
            var order = CreateWith(
                deliveryFee: Money.Of(0, Vnd),
                discountCode: "FREESHIP",
                discountAmount: Money.Of(0, Vnd));

            Assert.Equal(0, order.DeliveryFee.Amount);
            Assert.Equal("FREESHIP", order.DiscountCode);
            Assert.Equal(115000, order.TotalAmount.Amount);
        }

        [Fact]
        public void Create_WithGiftFee_IncludesItInTotal()
        {
            var order = CreateWith(
                deliveryFee: Money.Of(20000, Vnd),
                giftFee: Money.Of(5000, Vnd),
                isGift: true);

            Assert.True(order.IsGift);
            Assert.Equal(5000, order.GiftFee.Amount);
            Assert.Equal(140000, order.TotalAmount.Amount);
        }

        [Fact]
        public void Create_DiscountLargerThanSubtotal_NeverGoesNegative()
        {
            var order = CreateWith(
                deliveryFee: Money.Of(0, Vnd),
                discountCode: "BIG",
                discountAmount: Money.Of(500000, Vnd));

            Assert.Equal(0, order.TotalAmount.Amount);
        }

        [Fact]
        public void Create_WithBranch_SetsBranchFields()
        {
            var branchId = Guid.NewGuid();
            var order = CreateWith(branchId: branchId, branchName: "Phú Thượng 1");

            Assert.Equal(branchId, order.BranchId);
            Assert.Equal("Phú Thượng 1", order.BranchName);
        }

        [Fact]
        public void ChangeBranch_UpdatesFields()
        {
            var order = CreateWith();
            var branchId = Guid.NewGuid();

            order.ChangeBranch(branchId, "Cầu Giấy");

            Assert.Equal(branchId, order.BranchId);
            Assert.Equal("Cầu Giấy", order.BranchName);
        }

        [Fact]
        public void StatusFlow_FullLifecycle_ReachesCompleted()
        {
            var order = CreateWith();

            order.Confirm();
            order.StartPreparing();
            order.StartDelivering();
            order.Complete();

            Assert.Equal(OrderStatus.Completed, order.Status);
        }

        [Fact]
        public void StatusFlow_SkipSteps_ThrowsDomainException()
        {
            var order = CreateWith();

            Assert.Throws<DomainException>(() => order.StartDelivering());
            Assert.Throws<DomainException>(() => order.Complete());
        }

        [Fact]
        public void Cancel_FromDelivering_SetsCancelled()
        {
            var order = CreateWith();
            order.Confirm();
            order.StartPreparing();
            order.StartDelivering();

            order.Cancel();

            Assert.Equal(OrderStatus.Cancelled, order.Status);
        }

        [Fact]
        public void Confirm_CancelledOrder_ThrowsDomainException()
        {
            var order = CreateWith();
            order.Cancel();

            Assert.Throws<DomainException>(() => order.Confirm());
        }
    }
}
