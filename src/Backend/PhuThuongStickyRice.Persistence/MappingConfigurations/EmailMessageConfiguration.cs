using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
    {
        public void Configure(EntityTypeBuilder<EmailMessage> builder)
        {
            builder.ToTable("EmailMessages");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");
            // perf-hunt 2026-10-19: background worker SendEmailMessagesAsync polls
            // Where(SentDateTime == null) + OrderBy(NextAttemptDateTime) every cycle —
            // Take(100) limits results, not the scan; history of sent mails is kept
            // forever so the table grows unboundedly. Non-unique index on the
            // leading filter column: lookup only, no business change.
            builder.HasIndex(x => x.SentDateTime);
        }
    }
}