using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using Xunit;

namespace PhuThuongStickyRice.UnitTests.Domain.Entities
{
    public class OrderTests
    {
        private static Order CreateValidOrder()
        {
            return Order.Create(
                "PX-20261014-0001",
                "Nguyen Van A",
                "0901234567",
                null,
                "123 Lang Phu Thuong, Ha Noi",
                null,
                PaymentMethod.Cod,
                new List<(Guid, string, Money, int)>
                {
                    (Guid.NewGuid(), "Xoi gac", Money.Of(35000, "VND"), 2),
                    (Guid.NewGuid(), "Xoi xeo", Money.Of(25000, "VND"), 1)
                });
        }

        [Fact]
        public void Create_ValidData_SetsPropertiesAndPending()
        {
            var order = CreateValidOrder();

            Assert.Equal("PX-20261014-0001", order.OrderNumber);
            Assert.Equal("Nguyen Van A", order.CustomerName);
            Assert.Equal(OrderStatus.Pending, order.Status);
            Assert.Equal(2, order.Lines.Count);
            Assert.Equal(95000, order.TotalAmount.Amount);
            Assert.Equal("VND", order.TotalAmount.Currency);
        }

        [Fact]
        public void Create_EmptyLines_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() =>
                Order.Create("PX-1", "A", "090", null, "Addr", null, PaymentMethod.Cod,
                    new List<(Guid, string, Money, int)>()));
        }

        [Fact]
        public void Create_MixedCurrencies_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() =>
                Order.Create("PX-1", "A", "090", null, "Addr", null, PaymentMethod.Cod,
                    new List<(Guid, string, Money, int)>
                    {
                        (Guid.NewGuid(), "Xoi gac", Money.Of(35000, "VND"), 1),
                        (Guid.NewGuid(), "Xoi xeo", Money.Of(2, "USD"), 1)
                    }));
        }

        [Fact]
        public void StatusFlow_PendingToCompleted_FollowsTransitions()
        {
            var order = CreateValidOrder();

            order.Confirm();
            Assert.Equal(OrderStatus.Confirmed, order.Status);

            order.StartPreparing();
            Assert.Equal(OrderStatus.Preparing, order.Status);

            order.StartDelivering();
            Assert.Equal(OrderStatus.Delivering, order.Status);

            order.Complete();
            Assert.Equal(OrderStatus.Completed, order.Status);
        }

        [Fact]
        public void Complete_SkippingSteps_ThrowsDomainException()
        {
            var order = CreateValidOrder();

            Assert.Throws<DomainException>(() => order.Complete());
        }

        [Fact]
        public void Cancel_CompletedOrder_ThrowsDomainException()
        {
            var order = CreateValidOrder();
            order.Confirm();
            order.StartPreparing();
            order.StartDelivering();
            order.Complete();

            Assert.Throws<DomainException>(() => order.Cancel());
        }

        [Fact]
        public void Cancel_PendingOrder_SetsCancelled()
        {
            var order = CreateValidOrder();

            order.Cancel();

            Assert.Equal(OrderStatus.Cancelled, order.Status);
        }

        [Fact]
        public void Create_WithEmailAndPaymentMethod_SetsProperties()
        {
            var order = Order.Create(
                "PX-20261014-0002",
                "Nguyen Van B",
                "0901234567",
                "b@example.com",
                "123 Lang Phu Thuong, Ha Noi",
                null,
                PaymentMethod.BankTransfer,
                new List<(Guid, string, Money, int)>
                {
                    (Guid.NewGuid(), "Xoi gac", Money.Of(35000, "VND"), 1)
                });

            Assert.Equal("b@example.com", order.CustomerEmail);
            Assert.Equal(PaymentMethod.BankTransfer, order.PaymentMethod);
            Assert.Equal(PaymentStatus.Unpaid, order.PaymentStatus);
        }

        [Fact]
        public void Create_InvalidEmail_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() =>
                Order.Create("PX-1", "A", "090", "not-an-email", "Addr", null, PaymentMethod.Cod,
                    new List<(Guid, string, Money, int)>
                    {
                        (Guid.NewGuid(), "Xoi gac", Money.Of(35000, "VND"), 1)
                    }));
        }

        [Fact]
        public void Create_EmptyPhone_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() =>
                Order.Create("PX-1", "A", " ", null, "Addr", null, PaymentMethod.Cod,
                    new List<(Guid, string, Money, int)>
                    {
                        (Guid.NewGuid(), "Xoi gac", Money.Of(35000, "VND"), 1)
                    }));
        }

        [Fact]
        public void PaymentFlow_BankTransfer_MarkTransferredThenConfirmPayment()
        {
            var order = Order.Create(
                "PX-20261014-0003",
                "Nguyen Van C",
                "0901234567",
                null,
                "123 Lang Phu Thuong, Ha Noi",
                null,
                PaymentMethod.BankTransfer,
                new List<(Guid, string, Money, int)>
                {
                    (Guid.NewGuid(), "Xoi gac", Money.Of(35000, "VND"), 1)
                });

            order.MarkTransferred();
            Assert.Equal(PaymentStatus.PendingConfirmation, order.PaymentStatus);

            order.ConfirmPayment();
            Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        }

        [Fact]
        public void MarkTransferred_CodOrder_ThrowsDomainException()
        {
            var order = CreateValidOrder();

            Assert.Throws<DomainException>(() => order.MarkTransferred());
        }

        [Fact]
        public void ConfirmPayment_AlreadyPaid_ThrowsDomainException()
        {
            var order = CreateValidOrder();
            order.ConfirmPayment();

            Assert.Throws<DomainException>(() => order.ConfirmPayment());
        }

        [Fact]
        public void MarkPaymentFailed_PaidOrder_ThrowsDomainException()
        {
            var order = CreateValidOrder();
            order.ConfirmPayment();

            Assert.Throws<DomainException>(() => order.MarkPaymentFailed());
        }
    }
}
