using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRoles");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");
        }
    }
}
