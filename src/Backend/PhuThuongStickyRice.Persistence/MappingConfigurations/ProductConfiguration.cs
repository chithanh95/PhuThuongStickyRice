using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(Product.DescriptionMaxLength);
            builder.Property(x => x.CostPrice).HasPrecision(18, 2).IsRequired();

            // Item 256: giá khuyến mãi. Nullable — null = không có KM.
            builder.Property(x => x.PromoPrice).HasPrecision(18, 2);

            // Item 81: selling unit — "suất" (portion) or "kg" (per kilogram).
            builder.Property(x => x.Unit)
                .HasMaxLength(Product.UnitMaxLength)
                .IsRequired()
                .HasDefaultValue(Product.UnitPortion);

            // Item 164: daily sale time windows (JSON array of
            // {"from":"HH:mm","to":"HH:mm"}; null = sold all day).
            builder.Property(x => x.AvailableHoursJson)
                .HasMaxLength(Product.AvailableHoursJsonMaxLength);

            // Item 165: max portions per day; null = unlimited.
            builder.Property(x => x.DailyLimit);

            // Item 166: photo gallery as a JSON string array of image URLs.
            builder.Property(x => x.ImageUrlsJson)
                .HasMaxLength(Product.ImageUrlsJsonMaxLength);

            // Item 146: manual display order (drag & drop on /products).
            builder.Property(x => x.SortOrder).IsRequired().HasDefaultValue(0);

            builder.Property(x => x.Tags).HasMaxLength(Product.TagsMaxLength);
            builder.Property(x => x.Category).HasMaxLength(Product.CategoryMaxLength);
            builder.Property(x => x.AllergenInfo).HasMaxLength(Product.AllergenInfoMaxLength);
            builder.Property(x => x.IsNew).IsRequired();
            builder.Property(x => x.IsSignature).IsRequired();
            builder.Property(x => x.AvailableFrom);
            builder.Property(x => x.AvailableTo);

            // Multi-branch foundation (item 109): null = all branches.
            builder.Property(x => x.BranchId);
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.ComplexProperty(x => x.Code, codeBuilder =>
            {
                codeBuilder.Property(x => x.Value)
                    .HasColumnName("Code")
                    .HasMaxLength(Domain.ValueObjects.ProductCode.MaxLength)
                    .IsRequired();
            });

            builder.ComplexProperty(x => x.Price, priceBuilder =>
            {
                priceBuilder.Property(x => x.Amount)
                    .HasColumnName("Price")
                    .HasPrecision(18, 2)
                    .IsRequired();
                priceBuilder.Property(x => x.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // NOTE: EF Core does not support HasIndex() over complex-type members.
            // Code uniqueness is enforced by CreateProductCommand via IProductRepository.ExistsWithCodeAsync.
            builder.HasIndex(x => x.Status);
        }
    }
}
