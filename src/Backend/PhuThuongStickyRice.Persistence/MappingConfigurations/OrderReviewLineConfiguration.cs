using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class OrderReviewLineConfiguration : IEntityTypeConfiguration<OrderReviewLine>
    {
        public void Configure(EntityTypeBuilder<OrderReviewLine> builder)
        {
            builder.ToTable("OrderReviewLines");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.OrderReviewId).IsRequired();
            builder.Property(x => x.ProductId).IsRequired();
            builder.Property(x => x.ProductName)
                .HasMaxLength(OrderReviewLine.ProductNameMaxLength)
                .IsRequired();
            builder.Property(x => x.Rating).IsRequired();

            builder.HasOne(x => x.OrderReview)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.OrderReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ProductId);
        }
    }
}
