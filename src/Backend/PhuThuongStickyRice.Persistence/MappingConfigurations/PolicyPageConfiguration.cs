using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class PolicyPageConfiguration : IEntityTypeConfiguration<PolicyPage>
    {
        public void Configure(EntityTypeBuilder<PolicyPage> builder)
        {
            builder.ToTable("PolicyPages");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Slug)
                .HasMaxLength(PolicyPage.SlugMaxLength)
                .IsRequired();
            builder.HasIndex(x => x.Slug).IsUnique();

            builder.Property(x => x.Title)
                .HasMaxLength(PolicyPage.TitleMaxLength)
                .IsRequired();
            builder.Property(x => x.Body).IsRequired();
            builder.Property(x => x.SortOrder).IsRequired();
        }
    }
}
