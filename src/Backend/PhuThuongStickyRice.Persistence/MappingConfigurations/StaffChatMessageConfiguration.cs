using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    /// <summary>
    /// Cấu hình bảng StaffChatMessages (kênh chat nội bộ của staff).
    /// perf-hunt 2026-10-19 (run 14:25): entity này trước đây chưa có config
    /// class nên bảng không có index nào — StaffChatController.GetMessages
    /// chạy ORDER BY SentAt DESC + Take(100) mỗi lần staff mở/poll kênh chat,
    /// tức sort toàn bảng (bảng tăng vô hạn, không có retention). Index đơn
    /// trên SentAt phục vụ đúng query này, non-unique, lookup-only.
    /// </summary>
    public class StaffChatMessageConfiguration : IEntityTypeConfiguration<StaffChatMessage>
    {
        public void Configure(EntityTypeBuilder<StaffChatMessage> builder)
        {
            builder.ToTable("StaffChatMessages");
            builder.HasIndex(x => x.SentAt);
        }
    }
}
