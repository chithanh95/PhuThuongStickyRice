using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class CustomerTagConfiguration : IEntityTypeConfiguration<CustomerTag>
    {
        public void Configure(EntityTypeBuilder<CustomerTag> builder)
        {
            builder.ToTable("CustomerTags");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.CustomerPhone).HasMaxLength(CustomerTag.PhoneMaxLength).IsRequired();
            builder.Property(x => x.TagName).HasMaxLength(CustomerTag.TagNameMaxLength).IsRequired();
            builder.Property(x => x.Color).HasMaxLength(CustomerTag.ColorMaxLength);
            builder.Property(x => x.CreatedBy).HasMaxLength(CustomerTag.CreatedByMaxLength);

            builder.HasIndex(x => x.CustomerPhone);
            builder.HasIndex(x => new { x.CustomerPhone, x.TagName }).IsUnique();
        }
    }
}
