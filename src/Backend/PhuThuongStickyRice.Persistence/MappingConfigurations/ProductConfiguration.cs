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
        }
    }
}
