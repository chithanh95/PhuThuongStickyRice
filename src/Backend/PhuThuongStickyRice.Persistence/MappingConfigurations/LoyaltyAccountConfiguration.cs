using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
    {
        public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
        {
            builder.ToTable("LoyaltyAccounts");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Phone)
                .HasMaxLength(LoyaltyAccount.PhoneMaxLength)
                .IsRequired();
            builder.HasIndex(x => x.Phone).IsUnique();

            builder.Property(x => x.ReferralCode)
                .HasMaxLength(LoyaltyAccount.ReferralCodeMaxLength);
            builder.HasIndex(x => x.ReferralCode).IsUnique();

            builder.Property(x => x.Points).IsRequired();
            builder.Property(x => x.TotalEarned).IsRequired();
            builder.Property(x => x.TotalSpent).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
        }
    }
}
