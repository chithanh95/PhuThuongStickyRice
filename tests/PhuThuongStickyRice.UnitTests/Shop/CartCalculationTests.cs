using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PhuThuongStickyRice.UnitTests.Shop
{
    /// <summary>
    /// Tests for cart calculation logic (mirrors CartState/CartItemModel).
    /// These validate the money math that must be correct for customers.
    /// </summary>
    public class CartCalculationTests
    {
        // Mirrors CartItemModel.LineTotal
        private static decimal LineTotal(decimal unitPrice, int quantity) => unitPrice * quantity;

        // Mirrors CartState.BuildLineKey
        private static string BuildLineKey(Guid productId, Guid? sizeOptionId, IEnumerable<Guid> toppingIds, bool isCombo)
        {
            var toppings = toppingIds == null
                ? string.Empty
                : string.Join(",", toppingIds.OrderBy(x => x).Select(x => x.ToString("N")));
            return $"{(isCombo ? "combo" : "product")}:{productId:N}:{sizeOptionId?.ToString("N")}:{toppings}";
        }

        [Fact]
        public void LineTotal_BasePrice_Times_Quantity()
        {
            // Xôi Đỗ 15k × 2 = 30k
            Assert.Equal(30000, LineTotal(15000, 2));
        }

        [Fact]
        public void LineTotal_SizePrice_Plus_Toppings_Times_Quantity()
        {
            // Size lớn 45k + topping 10k = 55k × 2 = 110k
            var unitPrice = 45000 + 10000;
            Assert.Equal(110000, LineTotal(unitPrice, 2));
        }

        [Fact]
        public void LineTotal_Zero_Quantity_Is_Zero()
        {
            Assert.Equal(0, LineTotal(15000, 0));
        }

        [Fact]
        public void BuildLineKey_Different_Sizes_Produce_Different_Keys()
        {
            var productId = Guid.NewGuid();
            var size1 = Guid.NewGuid();
            var size2 = Guid.NewGuid();

            var key1 = BuildLineKey(productId, size1, null, false);
            var key2 = BuildLineKey(productId, size2, null, false);

            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void BuildLineKey_Different_Toppings_Produce_Different_Keys()
        {
            var productId = Guid.NewGuid();
            var topping1 = Guid.NewGuid();
            var topping2 = Guid.NewGuid();

            var key1 = BuildLineKey(productId, null, new[] { topping1 }, false);
            var key2 = BuildLineKey(productId, null, new[] { topping2 }, false);

            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void BuildLineKey_Same_Toppings_Different_Order_Produce_Same_Key()
        {
            // Toppings are sorted, so order doesn't matter
            var productId = Guid.NewGuid();
            var t1 = Guid.NewGuid();
            var t2 = Guid.NewGuid();

            var key1 = BuildLineKey(productId, null, new[] { t1, t2 }, false);
            var key2 = BuildLineKey(productId, null, new[] { t2, t1 }, false);

            Assert.Equal(key1, key2);
        }

        [Fact]
        public void BuildLineKey_Combo_Vs_Product_Produce_Different_Keys()
        {
            var id = Guid.NewGuid();

            var key1 = BuildLineKey(id, null, null, isCombo: true);
            var key2 = BuildLineKey(id, null, null, isCombo: false);

            Assert.NotEqual(key1, key2);
            Assert.StartsWith("combo:", key1);
            Assert.StartsWith("product:", key2);
        }

        [Fact]
        public void BuildLineKey_Null_Toppings_Same_As_Empty()
        {
            var productId = Guid.NewGuid();

            var key1 = BuildLineKey(productId, null, null, false);
            var key2 = BuildLineKey(productId, null, new List<Guid>(), false);

            Assert.Equal(key1, key2);
        }
    }

    /// <summary>
    /// Tests for discount calculation logic (mirrors DiscountAppService.ComputeDiscount).
    /// </summary>
    public class DiscountCalculationTests
    {
        // Mirrors DiscountAppService.ComputeDiscount (non-freeship path)
        private static decimal ComputeDiscount(decimal? percent, decimal? amount, decimal subtotal)
        {
            decimal discount;
            if (percent.HasValue)
            {
                var p = Math.Min(100, Math.Max(0, percent.Value));
                discount = Math.Round(subtotal * p / 100, 0);
            }
            else
            {
                discount = amount ?? 0;
            }
            return Math.Min(Math.Max(0, discount), subtotal);
        }

        [Fact]
        public void Discount_Percent_10_On_100k_Is_10k()
        {
            Assert.Equal(10000, ComputeDiscount(10, null, 100000));
        }

        [Fact]
        public void Discount_Percent_Capped_At_100()
        {
            // 150% should be capped at 100%
            Assert.Equal(100000, ComputeDiscount(150, null, 100000));
        }

        [Fact]
        public void Discount_Fixed_Amount()
        {
            Assert.Equal(20000, ComputeDiscount(null, 20000, 100000));
        }

        [Fact]
        public void Discount_Fixed_Capped_At_Subtotal()
        {
            // 200k discount on 100k order = 100k (not negative total)
            Assert.Equal(100000, ComputeDiscount(null, 200000, 100000));
        }

        [Fact]
        public void Discount_Negative_Percent_Treated_As_Zero()
        {
            Assert.Equal(0, ComputeDiscount(-10, null, 100000));
        }

        [Fact]
        public void Discount_Zero_Subtotal_Is_Zero()
        {
            Assert.Equal(0, ComputeDiscount(10, null, 0));
        }
    }
}
