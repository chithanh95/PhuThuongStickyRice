using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class QuickRatingConfiguration : IEntityTypeConfiguration<QuickRating>
    {
        public void Configure(EntityTypeBuilder<QuickRating> builder)
        {
            builder.ToTable("QuickRatings");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Rating).IsRequired();
            builder.Property(x => x.ProductId);
            builder.Property(x => x.CreatedAt).IsRequired();

            builder.HasIndex(x => x.ProductId);
        }
    }
}
