using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.SessionId).HasMaxLength(ChatMessage.SessionIdMaxLength).IsRequired();

            // perf-hunt 2026-10-19 (run 14:25): composite (SessionId, CreatedAt)
            // thay index đơn SessionId — endpoint public lịch sử chat
            // (LandingPublicController) luôn WHERE SessionId + ORDER BY
            // CreatedAt + Take(50); composite cho index seek rồi duyệt theo
            // thứ tự sẵn, không cần sort trên memory.
            builder.HasIndex(x => new { x.SessionId, x.CreatedAt });

            builder.Property(x => x.Sender).HasMaxLength(ChatMessage.SenderMaxLength).IsRequired();
            builder.Property(x => x.Message).HasMaxLength(ChatMessage.MessageMaxLength).IsRequired();
            builder.Property(x => x.IsRead).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
