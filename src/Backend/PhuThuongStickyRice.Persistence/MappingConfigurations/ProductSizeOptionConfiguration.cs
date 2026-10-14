using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class ProductSizeOptionConfiguration : IEntityTypeConfiguration<ProductSizeOption>
    {
        public void Configure(EntityTypeBuilder<ProductSizeOption> builder)
        {
            builder.ToTable("ProductSizeOptions");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.ProductId).IsRequired();
            builder.HasIndex(x => x.ProductId);

            builder.Property(x => x.Name).HasMaxLength(ProductSizeOption.NameMaxLength).IsRequired();
            builder.Property(x => x.Price).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
        }
    }
}
