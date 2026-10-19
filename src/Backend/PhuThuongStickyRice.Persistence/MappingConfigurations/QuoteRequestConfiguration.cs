using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class QuoteRequestConfiguration : IEntityTypeConfiguration<QuoteRequest>
    {
        public void Configure(EntityTypeBuilder<QuoteRequest> builder)
        {
            builder.ToTable("QuoteRequests");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.CustomerName).HasMaxLength(QuoteRequest.CustomerNameMaxLength).IsRequired();
            builder.Property(x => x.CustomerPhone).HasMaxLength(QuoteRequest.CustomerPhoneMaxLength).IsRequired();
            builder.Property(x => x.EventDate);
            builder.Property(x => x.EventLocation).HasMaxLength(QuoteRequest.EventLocationMaxLength);
            builder.Property(x => x.Budget).HasMaxLength(QuoteRequest.BudgetMaxLength);
            builder.Property(x => x.QuoteType).HasMaxLength(QuoteRequest.QuoteTypeMaxLength);
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(QuoteRequest.NotesMaxLength);
            builder.Property(x => x.Status).HasConversion<int>().IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();

            // perf-hunt 2026-10-19 (run 14:15): GetListAsync sort
            // OrderByDescending(CreatedAt) + LongCountAsync mỗi lần admin mở
            // trang báo giá; bảng tăng theo mỗi yêu cầu. Index non-unique,
            // lookup-only, không đổi nghiệp vụ.
            builder.HasIndex(x => x.CreatedAt);

            // perf-hunt 2026-10-19 (run 14:46): GetListAsync lọc
            // Where(Status) khi admin chọn trạng thái; không index thì
            // full scan toàn bảng. Index non-unique, lookup-only.
            builder.HasIndex(x => x.Status);
        }
    }
}
