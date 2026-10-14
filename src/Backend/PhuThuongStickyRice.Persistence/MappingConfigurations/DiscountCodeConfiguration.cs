using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
    {
        public void Configure(EntityTypeBuilder<DiscountCode> builder)
        {
            builder.ToTable("DiscountCodes");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Code)
                .HasMaxLength(DiscountCode.CodeMaxLength)
                .IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            builder.Property(x => x.MinOrderAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.UsedCount).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
        }
    }
}
