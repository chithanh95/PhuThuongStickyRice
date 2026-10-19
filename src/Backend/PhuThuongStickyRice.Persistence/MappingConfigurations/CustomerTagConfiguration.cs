using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class CustomerTagConfiguration : IEntityTypeConfiguration<CustomerTag>
    {
        public void Configure(EntityTypeBuilder<CustomerTag> builder)
        {
            builder.ToTable("CustomerTags");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.CustomerPhone).HasMaxLength(CustomerTag.PhoneMaxLength).IsRequired();
            builder.Property(x => x.TagName).HasMaxLength(CustomerTag.TagNameMaxLength).IsRequired();
            builder.Property(x => x.Color).HasMaxLength(CustomerTag.ColorMaxLength);
            builder.Property(x => x.CreatedBy).HasMaxLength(CustomerTag.CreatedByMaxLength);

            builder.HasIndex(x => x.CustomerPhone);
            builder.HasIndex(x => new { x.CustomerPhone, x.TagName }).IsUnique();

            // perf-hunt 2026-10-19 (run 14:25): composite (TagName, CreatedAt)
            // cho VipPriorityAppService.GetVipsAsync (WHERE TagName='VIP' +
            // ORDER BY CreatedAt DESC, KHÔNG Take, bảng tăng theo mỗi khách VIP)
            // và GetVipPhonesAsync (WHERE TagName='VIP' mỗi lần cache 5 phút
            // hết hạn). Index đơn CustomerPhone và unique (CustomerPhone,
            // TagName) đều không seek được theo TagName (cột thứ 2 của
            // composite) nên cả 2 query full scan + sort trên memory.
            builder.HasIndex(x => new { x.TagName, x.CreatedAt });
        }
    }
}
