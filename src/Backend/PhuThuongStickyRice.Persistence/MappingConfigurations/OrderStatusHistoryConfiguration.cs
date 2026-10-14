using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    /// <summary>
    /// Wave 6c (item 34): status-transition audit trail.
    /// </summary>
    public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
    {
        public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
        {
            builder.ToTable("OrderStatusHistory");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.OrderId).IsRequired();
            builder.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.OrderId);

            builder.Property(x => x.FromStatus)
                .HasConversion<int>()
                .IsRequired();
            builder.Property(x => x.ToStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.At).IsRequired();
            builder.Property(x => x.ByUser).HasMaxLength(OrderStatusHistory.ByUserMaxLength);
        }
    }
}
