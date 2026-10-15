using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermissions");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Role)
                .HasMaxLength(RolePermission.RoleMaxLength)
                .IsRequired();
            builder.Property(x => x.PermissionKey)
                .HasMaxLength(RolePermission.PermissionKeyMaxLength)
                .IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();

            builder.HasIndex(x => new { x.Role, x.PermissionKey }).IsUnique();
            builder.HasIndex(x => x.Role);
        }
    }
}
