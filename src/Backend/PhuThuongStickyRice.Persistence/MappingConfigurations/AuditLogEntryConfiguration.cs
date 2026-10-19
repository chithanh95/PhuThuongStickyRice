using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
    {
        public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
        {
            builder.ToTable("AuditLogEntries");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            // perf-hunt 2026-10-19: trang admin xem log sort + count theo
            // CreatedDateTime; bảng tăng theo mỗi lần tạo/sửa/xóa sản phẩm.
            builder.HasIndex(x => x.CreatedDateTime);
        }
    }
}
