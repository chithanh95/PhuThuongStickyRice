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

            builder.Property(x => x.SizeName).HasMaxLength(OrderLine.SizeNameMaxLength);
            builder.Property(x => x.ToppingNames).HasMaxLength(OrderLine.ToppingNamesMaxLength);
            builder.Property(x => x.ToppingsExtra).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.IsCombo).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(OrderLine.NoteMaxLength);

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

            builder.HasIndex(x => x.ProductId);
            // perf-hunt 2026-10-19 (run 17:25): EF Core KHONG tu tao index
            // cho FK (khac EF6) nen cot OrderId chua co index nao. Moi bao
            // cao join lines qua navigation (SelectMany x.Lines), export
            // Excel query WHERE OrderId IN (...), CustomerDetail topxxx
            // Where(l => ids.Contains(l.OrderId)) deu lookup theo OrderId.
            builder.HasIndex(x => x.OrderId);
        }
    }
}
