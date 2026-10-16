using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class PromoSubscriberConfiguration : IEntityTypeConfiguration<PromoSubscriber>
    {
        public void Configure(EntityTypeBuilder<PromoSubscriber> builder)
        {
            builder.ToTable("PromoSubscribers");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Phone).HasMaxLength(PromoSubscriber.PhoneMaxLength).IsRequired();
            builder.HasIndex(x => x.Phone).IsUnique();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.Source)
                .HasMaxLength(PromoSubscriber.SourceMaxLength)
                .IsRequired()
                .HasDefaultValue("landing");
        }
    }
}
