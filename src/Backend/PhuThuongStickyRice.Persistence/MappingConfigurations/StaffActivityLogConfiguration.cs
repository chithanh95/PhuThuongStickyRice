using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class StaffActivityLogConfiguration : IEntityTypeConfiguration<StaffActivityLog>
    {
        public void Configure(EntityTypeBuilder<StaffActivityLog> builder)
        {
            builder.ToTable("StaffActivityLogs");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.UserName)
                .HasMaxLength(StaffActivityLog.UserNameMaxLength)
                .IsRequired();
            builder.Property(x => x.Action)
                .HasMaxLength(StaffActivityLog.ActionMaxLength)
                .IsRequired();
            builder.Property(x => x.Detail)
                .HasMaxLength(StaffActivityLog.DetailMaxLength);
            builder.Property(x => x.CreatedAt).IsRequired();

            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Action);
        }
    }
}
