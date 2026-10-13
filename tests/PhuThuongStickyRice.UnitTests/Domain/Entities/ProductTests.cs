using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.ValueObjects;
using Xunit;

namespace PhuThuongStickyRice.UnitTests.Domain.Entities
{
    public class ProductTests
    {
        private static Product CreateValidProduct()
        {
            return Product.Create(ProductCode.Of("XOI-001"), "Xoi Phu Thuong", Money.Of(25000, "VND"), "Xoi xeo");
        }

        [Fact]
        public void Create_ValidData_SetsPropertiesAndActivates()
        {
            var product = CreateValidProduct();

            Assert.Equal("XOI-001", product.Code.Value);
            Assert.Equal("Xoi Phu Thuong", product.Name);
            Assert.Equal("Xoi xeo", product.Description);
            Assert.Equal(25000, product.Price.Amount);
            Assert.Equal("VND", product.Price.Currency);
            Assert.Equal(ProductStatus.Active, product.Status);
        }

        [Fact]
        public void Create_LowercaseCode_NormalizesToUppercase()
        {
            var product = Product.Create(ProductCode.Of("xoi-002"), "Xoi", Money.Of(10000, "VND"));

            Assert.Equal("XOI-002", product.Code.Value);
        }

        [Fact]
        public void Create_EmptyName_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() =>
                Product.Create(ProductCode.Of("XOI-003"), "  ", Money.Of(10000, "VND")));
        }

        [Fact]
        public void Create_NegativePrice_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() =>
                Product.Create(ProductCode.Of("XOI-004"), "Xoi", Money.Of(-1, "VND")));
        }

        [Fact]
        public void UpdateDetails_ValidData_UpdatesNameAndDescription()
        {
            var product = CreateValidProduct();

            product.UpdateDetails("Xoi moi", "Mo ta moi");

            Assert.Equal("Xoi moi", product.Name);
            Assert.Equal("Mo ta moi", product.Description);
        }

        [Fact]
        public void UpdateDetails_DiscontinuedProduct_ThrowsDomainException()
        {
            var product = CreateValidProduct();
            product.Discontinue();

            Assert.Throws<DomainException>(() => product.UpdateDetails("Xoi", "Mo ta"));
        }

        [Fact]
        public void ChangePrice_ValidPrice_UpdatesPrice()
        {
            var product = CreateValidProduct();

            product.ChangePrice(Money.Of(30000, "VND"));

            Assert.Equal(30000, product.Price.Amount);
        }

        [Fact]
        public void StatusTransitions_FollowRules()
        {
            var product = CreateValidProduct();

            product.Deactivate();
            Assert.Equal(ProductStatus.Inactive, product.Status);

            product.Activate();
            Assert.Equal(ProductStatus.Active, product.Status);

            product.Discontinue();
            Assert.Equal(ProductStatus.Discontinued, product.Status);

            Assert.Throws<DomainException>(() => product.Activate());
        }

        [Fact]
        public void ProductCode_EmptyValue_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => ProductCode.Of(" "));
        }

        [Fact]
        public void ProductCode_TooLong_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => ProductCode.Of(new string('X', ProductCode.MaxLength + 1)));
        }

        [Fact]
        public void Money_InvalidCurrency_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => Money.Of(100, "VNDD"));
            Assert.Throws<DomainException>(() => Money.Of(100, ""));
        }
    }
}
