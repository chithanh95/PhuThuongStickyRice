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
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(QuoteRequest.NotesMaxLength);
            builder.Property(x => x.Status).HasConversion<int>().IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
