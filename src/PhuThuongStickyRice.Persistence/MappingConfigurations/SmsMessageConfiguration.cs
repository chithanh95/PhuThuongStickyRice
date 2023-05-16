using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class SmsMessageConfiguration : IEntityTypeConfiguration<SmsMessage>
    {
        public void Configure(EntityTypeBuilder<SmsMessage> builder)
        {
            builder.ToTable("SmsMessages");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");
        }
    }
}
