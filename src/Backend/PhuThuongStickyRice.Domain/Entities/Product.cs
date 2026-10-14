using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Linq;

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
        public const int TagsMaxLength = 200;
        public const int CategoryMaxLength = 100;
        public const int AllergenInfoMaxLength = 500;

        public ProductCode Code { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public Money Price { get; private set; }

        /// <summary>
        /// Cost price (giá vốn) in VND, admin-editable. Powers the
        /// /admin/loi-nhuan profit report (revenue − cost).
        /// </summary>
        public decimal CostPrice { get; private set; }

        public ProductStatus Status { get; private set; }

        /// <summary>
        /// Dietary tags as a comma-separated list, e.g. "chay,man" or "cay".
        /// Supported tags: "chay" (vegetarian), "man" (savory), "cay" (spicy).
        /// </summary>
        public string Tags { get; private set; }

        /// <summary>Menu category, e.g. "Xôi mặn", "Xôi ngọt", "Món ăn kèm".</summary>
        public string Category { get; private set; }

        /// <summary>Allergen information shown in the quick-view, e.g. "đậu phộng, tôm".</summary>
        public string AllergenInfo { get; private set; }

        /// <summary>Shows a "Món mới" badge on the shop.</summary>
        public bool IsNew { get; private set; }

        /// <summary>Shows a "Signature" badge on the shop.</summary>
        public bool IsSignature { get; private set; }

        /// <summary>
        /// Seasonal availability window. Null = no restriction. The public
        /// shop hides products outside the window; the admin list still
        /// shows them with an "out of season" badge.
        /// </summary>
        public DateTime? AvailableFrom { get; private set; }

        public DateTime? AvailableTo { get; private set; }

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

        /// <summary>Sets the cost price (giá vốn) in VND. Must not be negative.</summary>
        public void ChangeCostPrice(decimal costPrice)
        {
            if (costPrice < 0)
            {
                throw new DomainException("Cost price must not be negative.");
            }

            CostPrice = costPrice;
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

        /// <summary>
        /// Updates the marketing/catalog fields: dietary tags, category,
        /// allergen info, badges and the seasonal availability window.
        /// </summary>
        public void UpdateCatalogInfo(
            string tags,
            string category,
            string allergenInfo,
            bool isNew,
            bool isSignature,
            DateTime? availableFrom,
            DateTime? availableTo)
        {
            if (Status == ProductStatus.Discontinued)
            {
                throw new DomainException("Cannot update a discontinued product.");
            }

            var normalizedTags = NormalizeTags(tags);
            if (normalizedTags != null && normalizedTags.Length > TagsMaxLength)
            {
                throw new DomainException($"Product tags must not exceed {TagsMaxLength} characters.");
            }

            if (category != null && category.Trim().Length > CategoryMaxLength)
            {
                throw new DomainException($"Product category must not exceed {CategoryMaxLength} characters.");
            }

            if (allergenInfo != null && allergenInfo.Trim().Length > AllergenInfoMaxLength)
            {
                throw new DomainException($"Allergen info must not exceed {AllergenInfoMaxLength} characters.");
            }

            if (availableFrom.HasValue && availableTo.HasValue && availableFrom.Value.Date > availableTo.Value.Date)
            {
                throw new DomainException("AvailableFrom must not be after AvailableTo.");
            }

            Tags = normalizedTags;
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
            AllergenInfo = string.IsNullOrWhiteSpace(allergenInfo) ? null : allergenInfo.Trim();
            IsNew = isNew;
            IsSignature = isSignature;
            AvailableFrom = availableFrom?.Date;
            AvailableTo = availableTo?.Date;
        }

        /// <summary>
        /// True when the product may be shown on the public shop right now:
        /// active status and inside the seasonal window (when one is set).
        /// </summary>
        public bool IsAvailableNow(DateTime utcNow)
        {
            if (Status != ProductStatus.Active)
            {
                return false;
            }

            var today = utcNow.Date;
            if (AvailableFrom.HasValue && today < AvailableFrom.Value.Date)
            {
                return false;
            }

            if (AvailableTo.HasValue && today > AvailableTo.Value.Date)
            {
                return false;
            }

            return true;
        }

        private static string NormalizeTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return null;
            }

            var parts = tags.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().ToLowerInvariant())
                .Where(x => x.Length > 0)
                .Distinct()
                .ToList();

            return parts.Count == 0 ? null : string.Join(",", parts);
        }
    }
}
