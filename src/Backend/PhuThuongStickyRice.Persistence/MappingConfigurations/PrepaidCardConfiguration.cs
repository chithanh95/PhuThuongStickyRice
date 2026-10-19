using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    /// <summary>
    /// Cấu hình entity PrepaidCard (thẻ xôi sáng trả trước).
    /// </summary>
    public class PrepaidCardConfiguration : IEntityTypeConfiguration<PrepaidCard>
    {
        /// <summary>Cấu hình mapping cho entity PrepaidCard.</summary>
        /// <param name="builder">Entity type builder.</param>
        public void Configure(EntityTypeBuilder<PrepaidCard> builder)
        {
            // perf-hunt 2026-10-19 (run 18:05): lookup CardCode là thao tác
            // real-time tại quầy — PrepaidCardRepository.TryRedeemAsync
            // (ExecuteUpdateAsync nguyên tử) + PrepaidCardAppService.
            // RedeemAsync (2 lần FirstOrDefaultAsync khi trừ thất bại hoặc
            // đọc lại sau trừ) + GenerateUniqueCodeAsync (check trùng khi
            // tạo thẻ) + GetListAsync (tìm kiếm Contains theo mã). Bảng
            // trước đây không có configuration class nên mọi lookup theo
            // CardCode đều là clustered index scan; unique index cho seek
            // equality. Uniqueness khớp đúng ngữ nghĩa nghiệp vụ (mã luôn
            // "TX"+6 số in hoa, GenerateUniqueCodeAsync đảm bảo duy nhất;
            // SQL Server collation mặc định CI nên == hiện tại đã so sánh
            // case-insensitive, index không đổi ngữ nghĩa). Lookup-only,
            // không đổi nghiệp vụ/API.
            builder.HasIndex(x => x.CardCode)
                .IsUnique()
                .HasDatabaseName("IX_PrepaidCards_CardCode");
        }
    }
}
