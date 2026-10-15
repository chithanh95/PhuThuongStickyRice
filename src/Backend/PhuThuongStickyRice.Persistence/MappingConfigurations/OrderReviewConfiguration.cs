using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class OrderReviewConfiguration : IEntityTypeConfiguration<OrderReview>
    {
        public void Configure(EntityTypeBuilder<OrderReview> builder)
        {
            builder.ToTable("OrderReviews");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.OrderId).IsRequired();
            builder.Property(x => x.OrderNumber)
                .HasMaxLength(OrderReview.OrderNumberMaxLength)
                .IsRequired();
            builder.HasIndex(x => x.OrderId).IsUnique();

            builder.Property(x => x.CustomerPhone)
                .HasMaxLength(OrderReview.PhoneMaxLength)
                .IsRequired();
            builder.Property(x => x.Rating).IsRequired();
            builder.Property(x => x.Comment).HasMaxLength(OrderReview.CommentMaxLength);
            builder.Property(x => x.PhotoUrl).HasMaxLength(500);
            builder.Property(x => x.RewardCode).HasMaxLength(50);
            builder.Property(x => x.IsHidden).IsRequired();
            builder.Property(x => x.AdminReply).HasMaxLength(OrderReview.AdminReplyMaxLength);
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
