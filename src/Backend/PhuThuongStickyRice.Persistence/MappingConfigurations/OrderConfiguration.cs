using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.OrderNumber)
                .HasMaxLength(Order.OrderNumberMaxLength)
                .IsRequired();
            builder.HasIndex(x => x.OrderNumber).IsUnique();

            builder.Property(x => x.CustomerName).HasMaxLength(Order.CustomerNameMaxLength).IsRequired();
            builder.Property(x => x.CustomerPhone).HasMaxLength(Order.CustomerPhoneMaxLength).IsRequired();
            builder.Property(x => x.CustomerAddress).HasMaxLength(Order.CustomerAddressMaxLength).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(Order.NotesMaxLength);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAt).IsRequired();

            builder.ComplexProperty(x => x.TotalAmount, totalBuilder =>
            {
                totalBuilder.Property(x => x.Amount)
                    .HasColumnName("TotalAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();
                totalBuilder.Property(x => x.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Lines).AutoInclude();
        }
    }
}
