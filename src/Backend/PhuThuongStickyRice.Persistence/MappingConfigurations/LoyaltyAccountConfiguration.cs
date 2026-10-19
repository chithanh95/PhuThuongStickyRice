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

            // perf-hunt 2026-10-19 (run 14:46): LoyaltyAppService.GetListAsync
            // chạy OrderByDescending(TotalSpent) + Take(200) mỗi lần admin mở
            // trang tài khoản điểm; bảng tăng theo mỗi đơn hàng. Index
            // non-unique, lookup-only, không đổi nghiệp vụ.
            builder.HasIndex(x => x.TotalSpent);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
        }
    }
}
