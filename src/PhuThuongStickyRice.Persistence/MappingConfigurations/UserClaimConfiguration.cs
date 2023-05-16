using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
    {
        public void Configure(EntityTypeBuilder<UserClaim> builder)
        {
            builder.ToTable("UserClaims");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");
        }
    }
}
