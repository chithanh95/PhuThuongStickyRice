using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class OrderRewardRuleConfiguration : IEntityTypeConfiguration<OrderRewardRule>
    {
        public void Configure(EntityTypeBuilder<OrderRewardRule> builder)
        {
            builder.ToTable("OrderRewardRules");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Name).HasMaxLength(OrderRewardRule.NameMaxLength).IsRequired();
            builder.Property(x => x.MinAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.RewardType).HasConversion<int>().IsRequired();
            builder.Property(x => x.ToppingName).HasMaxLength(OrderRewardRule.ToppingNameMaxLength);
            builder.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
