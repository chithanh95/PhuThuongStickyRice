using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class BlacklistedPhoneConfiguration : IEntityTypeConfiguration<BlacklistedPhone>
    {
        public void Configure(EntityTypeBuilder<BlacklistedPhone> builder)
        {
            builder.ToTable("BlacklistedPhones");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Phone).HasMaxLength(BlacklistedPhone.PhoneMaxLength).IsRequired();
            builder.Property(x => x.NormalizedPhone).HasMaxLength(BlacklistedPhone.PhoneMaxLength).IsRequired();
            builder.HasIndex(x => x.NormalizedPhone).IsUnique();
            builder.Property(x => x.Reason).HasMaxLength(BlacklistedPhone.ReasonMaxLength);
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
