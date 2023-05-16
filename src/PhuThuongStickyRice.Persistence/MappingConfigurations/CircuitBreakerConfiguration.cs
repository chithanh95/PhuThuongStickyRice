using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhuThuongStickyRice.Persistence.CircuitBreakers;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class CircuitBreakerConfiguration : IEntityTypeConfiguration<CircuitBreaker>
    {
        public void Configure(EntityTypeBuilder<CircuitBreaker> builder)
        {
            builder.ToTable("CircuitBreakers");
            builder.HasIndex(x => new { x.Name }).IsUnique();
        }
    }
}
