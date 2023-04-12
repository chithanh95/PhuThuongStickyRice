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
        }
    }
}