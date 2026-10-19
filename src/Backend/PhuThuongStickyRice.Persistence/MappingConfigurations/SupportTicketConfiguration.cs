using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
    {
        public void Configure(EntityTypeBuilder<SupportTicket> builder)
        {
            builder.ToTable("SupportTickets");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Name)
                .HasMaxLength(SupportTicket.NameMaxLength)
                .IsRequired();
            builder.Property(x => x.Phone)
                .HasMaxLength(SupportTicket.PhoneMaxLength)
                .IsRequired();
            builder.Property(x => x.OrderNumber)
                .HasMaxLength(SupportTicket.OrderNumberMaxLength);
            builder.Property(x => x.Message)
                .HasMaxLength(SupportTicket.MessageMaxLength)
                .IsRequired();
            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();
            builder.Property(x => x.ReplyNote)
                .HasMaxLength(SupportTicket.ReplyNoteMaxLength);
            builder.Property(x => x.CreatedAt).IsRequired();
            // perf-hunt 2026-10-19: admin lọc ticket theo Status + sort
            // CreatedAt desc (GetListAsync), bảng chưa có index nào.
            builder.HasIndex(x => new { x.Status, x.CreatedAt });
        }
    }
}
