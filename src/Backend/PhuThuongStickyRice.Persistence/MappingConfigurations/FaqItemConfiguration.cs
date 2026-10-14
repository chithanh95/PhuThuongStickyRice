using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class FaqItemConfiguration : IEntityTypeConfiguration<FaqItem>
    {
        public void Configure(EntityTypeBuilder<FaqItem> builder)
        {
            builder.ToTable("FaqItems");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Question)
                .HasMaxLength(FaqItem.QuestionMaxLength)
                .IsRequired();
            builder.Property(x => x.Answer)
                .HasMaxLength(FaqItem.AnswerMaxLength)
                .IsRequired();
            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
        }
    }
}
