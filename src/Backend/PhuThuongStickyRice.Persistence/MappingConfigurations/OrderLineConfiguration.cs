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

            // Gom 2 cột tiền về MoneyColumnMapper (trước đây copy khối
            // ComplexProperty giống hệt nhau) — mapping giữ nguyên.
            MoneyColumnMapper.ConfigureMoneyColumn(builder, x => x.UnitPrice, "UnitPrice", "UnitPriceCurrency");
            MoneyColumnMapper.ConfigureMoneyColumn(builder, x => x.LineTotal, "LineTotal", "LineTotalCurrency");

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
