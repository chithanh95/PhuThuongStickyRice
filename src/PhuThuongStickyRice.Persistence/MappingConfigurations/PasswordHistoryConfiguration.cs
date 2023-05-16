using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
    {
        public void Configure(EntityTypeBuilder<PasswordHistory> builder)
        {
            builder.ToTable("PasswordHistories");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");
        }
    }
}
