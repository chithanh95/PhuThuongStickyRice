using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
    {
        public void Configure(EntityTypeBuilder<Referral> builder)
        {
            builder.ToTable("Referrals");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.ReferrerPhone).HasMaxLength(Referral.PhoneMaxLength).IsRequired();
            builder.Property(x => x.ReferredPhone).HasMaxLength(Referral.PhoneMaxLength).IsRequired();
            // A referrer can invite the same friend only once.
            builder.HasIndex(x => new { x.ReferrerPhone, x.ReferredPhone }).IsUnique();

            builder.Property(x => x.RewardAmount).IsRequired();
            builder.Property(x => x.IsRewarded).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
