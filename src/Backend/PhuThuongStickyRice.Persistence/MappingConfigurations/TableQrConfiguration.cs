using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class TableQrConfiguration : IEntityTypeConfiguration<TableQr>
    {
        public void Configure(EntityTypeBuilder<TableQr> builder)
        {
            builder.ToTable("TableQrs");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Label).HasMaxLength(TableQr.LabelMaxLength).IsRequired();
            builder.HasIndex(x => x.Label).IsUnique();
            builder.Property(x => x.EncodedUrl).HasMaxLength(TableQr.UrlMaxLength).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
