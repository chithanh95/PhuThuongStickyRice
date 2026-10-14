using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class DishCandidateConfiguration : IEntityTypeConfiguration<DishCandidate>
    {
        public void Configure(EntityTypeBuilder<DishCandidate> builder)
        {
            builder.ToTable("DishCandidates");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Name).HasMaxLength(DishCandidate.NameMaxLength).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(DishCandidate.DescriptionMaxLength);
            builder.Property(x => x.Votes).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
