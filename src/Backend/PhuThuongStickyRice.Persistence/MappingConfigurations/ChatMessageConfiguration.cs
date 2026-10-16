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
            builder.HasIndex(x => x.SessionId);

            builder.Property(x => x.Sender).HasMaxLength(ChatMessage.SenderMaxLength).IsRequired();
            builder.Property(x => x.Message).HasMaxLength(ChatMessage.MessageMaxLength).IsRequired();
            builder.Property(x => x.IsRead).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
