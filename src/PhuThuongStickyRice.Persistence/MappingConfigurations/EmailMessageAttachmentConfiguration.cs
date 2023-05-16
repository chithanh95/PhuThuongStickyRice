using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class EmailMessageAttachmentConfiguration : IEntityTypeConfiguration<EmailMessageAttachment>
    {
        public void Configure(EntityTypeBuilder<EmailMessageAttachment> builder)
        {
            builder.ToTable("EmailMessageAttachments");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");
        }
    }
}
