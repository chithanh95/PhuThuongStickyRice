using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class OrderMessageTemplateConfiguration : IEntityTypeConfiguration<OrderMessageTemplate>
    {
        public void Configure(EntityTypeBuilder<OrderMessageTemplate> builder)
        {
            builder.ToTable("OrderMessageTemplates");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Status)
                .HasMaxLength(OrderMessageTemplate.StatusMaxLength)
                .IsRequired();
            builder.HasIndex(x => x.Status).IsUnique();

            builder.Property(x => x.Template)
                .HasMaxLength(OrderMessageTemplate.TemplateMaxLength)
                .IsRequired();

            builder.Property(x => x.IsActive).IsRequired();
        }
    }
}
