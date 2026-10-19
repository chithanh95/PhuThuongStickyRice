using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class LoginAuditConfiguration : IEntityTypeConfiguration<LoginAudit>
    {
        public void Configure(EntityTypeBuilder<LoginAudit> builder)
        {
            builder.ToTable("LoginAudits");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.UserName).HasMaxLength(LoginAudit.UserNameMaxLength).IsRequired();
            builder.HasIndex(x => x.UserName);
            // perf-hunt 2026-10-19: trang admin xem login audit sort +
            // LongCount theo At mỗi lần mở; chỉ có index UserName -> sort/scan cả bảng.
            builder.HasIndex(x => x.At);
            builder.Property(x => x.At).IsRequired();
            builder.Property(x => x.IpAddress).HasMaxLength(LoginAudit.IpMaxLength);
            builder.Property(x => x.Success).IsRequired();
            builder.Property(x => x.FailureReason).HasMaxLength(LoginAudit.ReasonMaxLength);
        }
    }
}
