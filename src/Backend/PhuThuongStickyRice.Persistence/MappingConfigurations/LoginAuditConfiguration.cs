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
            builder.Property(x => x.At).IsRequired();
            builder.Property(x => x.IpAddress).HasMaxLength(LoginAudit.IpMaxLength);
            builder.Property(x => x.Success).IsRequired();
            builder.Property(x => x.FailureReason).HasMaxLength(LoginAudit.ReasonMaxLength);
        }
    }
}
