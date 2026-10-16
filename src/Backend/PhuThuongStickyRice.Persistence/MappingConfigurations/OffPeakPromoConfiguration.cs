using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class OffPeakPromoConfiguration : IEntityTypeConfiguration<OffPeakPromo>
    {
        public void Configure(EntityTypeBuilder<OffPeakPromo> builder)
        {
            builder.ToTable("OffPeakPromos");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");
            builder.Property(x => x.StartHour).IsRequired();
            builder.Property(x => x.EndHour).IsRequired();
            builder.Property(x => x.DiscountAmount).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
