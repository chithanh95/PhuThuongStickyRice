using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
    {
        public void Configure(EntityTypeBuilder<OrderLine> builder)
        {
            builder.ToTable("OrderLines");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.ProductId).IsRequired();
            builder.Property(x => x.ProductName).HasMaxLength(Product.NameMaxLength).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();

            builder.ComplexProperty(x => x.UnitPrice, priceBuilder =>
            {
                priceBuilder.Property(x => x.Amount)
                    .HasColumnName("UnitPrice")
                    .HasPrecision(18, 2)
                    .IsRequired();
                priceBuilder.Property(x => x.Currency)
                    .HasColumnName("UnitPriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.ComplexProperty(x => x.LineTotal, totalBuilder =>
            {
                totalBuilder.Property(x => x.Amount)
                    .HasColumnName("LineTotal")
                    .HasPrecision(18, 2)
                    .IsRequired();
                totalBuilder.Property(x => x.Currency)
                    .HasColumnName("LineTotalCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        }
    }
}
