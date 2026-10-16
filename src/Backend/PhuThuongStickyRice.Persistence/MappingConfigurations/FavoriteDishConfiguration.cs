using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class FavoriteDishConfiguration : IEntityTypeConfiguration<FavoriteDish>
    {
        public void Configure(EntityTypeBuilder<FavoriteDish> builder)
        {
            builder.ToTable("FavoriteDishes");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");
            builder.Property(x => x.Phone).HasMaxLength(FavoriteDish.PhoneMaxLength).IsRequired();
            builder.Property(x => x.ProductId).IsRequired();
            // One favorite per phone per product.
            builder.HasIndex(x => new { x.Phone, x.ProductId }).IsUnique();
            builder.Property(x => x.CreatedAt).IsRequired();

            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
