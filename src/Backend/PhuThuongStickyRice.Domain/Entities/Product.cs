using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Product aggregate root. All state changes go through intention-revealing
    /// behaviors so domain invariants are enforced in one place.
    /// </summary>
    public class Product : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;
        public const int DescriptionMaxLength = 2000;

        public ProductCode Code { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public Money Price { get; private set; }

        public ProductStatus Status { get; private set; }

        // EF Core
        private Product()
        {
        }

        private Product(ProductCode code, string name, Money price, string description)
        {
            Code = code;
            Name = name;
            Price = price;
            Description = description;
            Status = ProductStatus.Active;
        }

        public static Product Create(ProductCode code, string name, Money price, string description = null)
        {
            if (code == null)
            {
                throw new DomainException("Product code is required.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new DomainException("Product name must not be empty.");
            }

            if (name.Trim().Length > NameMaxLength)
            {
                throw new DomainException($"Product name must not exceed {NameMaxLength} characters.");
            }

            if (price == null)
            {
                throw new DomainException("Product price is required.");
            }

            if (description != null && description.Length > DescriptionMaxLength)
            {
                throw new DomainException($"Product description must not exceed {DescriptionMaxLength} characters.");
            }

            return new Product(code, name.Trim(), price, description?.Trim());
        }

        public void UpdateDetails(string name, string description)
        {
            if (Status == ProductStatus.Discontinued)
            {
                throw new DomainException("Cannot update a discontinued product.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new DomainException("Product name must not be empty.");
            }

            if (name.Trim().Length > NameMaxLength)
            {
                throw new DomainException($"Product name must not exceed {NameMaxLength} characters.");
            }

            if (description != null && description.Length > DescriptionMaxLength)
            {
                throw new DomainException($"Product description must not exceed {DescriptionMaxLength} characters.");
            }

            Name = name.Trim();
            Description = description?.Trim();
        }

        public void ChangePrice(Money newPrice)
        {
            if (newPrice == null)
            {
                throw new DomainException("Product price is required.");
            }

            if (Status == ProductStatus.Discontinued)
            {
                throw new DomainException("Cannot change the price of a discontinued product.");
            }

            Price = newPrice;
        }

        public void Activate()
        {
            if (Status == ProductStatus.Discontinued)
            {
                throw new DomainException("A discontinued product cannot be re-activated.");
            }

            Status = ProductStatus.Active;
        }

        public void Deactivate()
        {
            if (Status == ProductStatus.Discontinued)
            {
                throw new DomainException("Product is already discontinued.");
            }

            Status = ProductStatus.Inactive;
        }

        public void Discontinue()
        {
            Status = ProductStatus.Discontinued;
        }
    }
}
