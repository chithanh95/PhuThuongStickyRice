using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
    {
        public void Configure(EntityTypeBuilder<DiscountCode> builder)
        {
            builder.ToTable("DiscountCodes");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Code)
                .HasMaxLength(DiscountCode.CodeMaxLength)
                .IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();

            builder.HasIndex(x => new { x.IsActive, x.CreatedDateTime })        .HasDatabaseName("IX_DiscountCodes_IsActive_CreatedDateTime");

            // perf-hunt 2026-10-19 (run 14:15): admin list sort TRẦN theo
            // CreatedDateTime DESC + Take(500), không WHERE — composite
            // (IsActive, CreatedDateTime) không phục vụ ORDER BY trần cột
            // thứ 2 nên phải sort toàn bảng. Index đơn riêng cho ORDER BY.
            // Bảng tăng vô hạn (spin/sinh nhật/reward-rule). Non-unique,
            // lookup-only, không đổi nghiệp vụ.
            builder.HasIndex(x => x.CreatedDateTime);

            // perf-hunt 2026-10-19 (run 13:55): GetUsableCodeAsync (OwnerPhone ==
            // phone, public checkout path) và GetPhonesWithUsableCodeAsync
            // (OwnerPhone IN (...), batch sinh nhật) đều lọc Code.StartsWith(prefix)
            // — chỉ có unique index trên Code nên phải range-scan mọi mã cùng prefix.
            // Composite này cho seek equality trên (OwnerPhone, IsActive) rồi range
            // trên Code; non-unique, không đổi nghiệp vụ.
            builder.HasIndex(x => new { x.OwnerPhone, x.IsActive, x.Code })
                .HasDatabaseName("IX_DiscountCodes_OwnerPhone_IsActive_Code");

            builder.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            builder.Property(x => x.MinOrderAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.UsedCount).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();

            builder.Property(x => x.OwnerPhone).HasMaxLength(30);
            builder.Property(x => x.VoucherType).IsRequired();
        }
    }
}
