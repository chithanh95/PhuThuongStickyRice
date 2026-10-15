using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class ProductChangeLogConfiguration : IEntityTypeConfiguration<ProductChangeLog>
    {
        public void Configure(EntityTypeBuilder<ProductChangeLog> builder)
        {
            builder.ToTable("ProductChangeLogs");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.ProductId).IsRequired();
            builder.HasIndex(x => x.ProductId);

            builder.Property(x => x.ChangedBy).HasMaxLength(ProductChangeLog.ChangedByMaxLength);
            builder.Property(x => x.ChangedAt).IsRequired();
            builder.HasIndex(x => x.ChangedAt);

            builder.Property(x => x.FieldName).HasMaxLength(ProductChangeLog.FieldNameMaxLength).IsRequired();
            builder.Property(x => x.OldValue).HasMaxLength(ProductChangeLog.ValueMaxLength);
            builder.Property(x => x.NewValue).HasMaxLength(ProductChangeLog.ValueMaxLength);
        }
    }
}
