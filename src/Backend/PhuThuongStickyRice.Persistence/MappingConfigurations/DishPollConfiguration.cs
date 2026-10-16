using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class DishPollConfiguration : IEntityTypeConfiguration<DishPoll>
    {
        public void Configure(EntityTypeBuilder<DishPoll> builder)
        {
            builder.ToTable("DishPolls");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");
            builder.Property(x => x.DishName).HasMaxLength(DishPoll.DishNameMaxLength).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(DishPoll.DescriptionMaxLength);
            builder.Property(x => x.ImageUrl).HasMaxLength(DishPoll.ImageUrlMaxLength);
            builder.Property(x => x.VoteCount).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
