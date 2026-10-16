using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class DishVoteConfiguration : IEntityTypeConfiguration<DishVote>
    {
        public void Configure(EntityTypeBuilder<DishVote> builder)
        {
            builder.ToTable("DishVotes");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");
            builder.Property(x => x.PollId).IsRequired();
            builder.Property(x => x.Phone).HasMaxLength(DishVote.PhoneMaxLength).IsRequired();
            // One vote per phone per poll.
            builder.HasIndex(x => new { x.PollId, x.Phone }).IsUnique();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
