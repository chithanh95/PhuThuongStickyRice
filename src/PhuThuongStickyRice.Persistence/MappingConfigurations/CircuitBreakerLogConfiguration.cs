using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhuThuongStickyRice.Persistence.CircuitBreakers;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class CircuitBreakerLogConfiguration : IEntityTypeConfiguration<CircuitBreakerLog>
    {
        public void Configure(EntityTypeBuilder<CircuitBreakerLog> builder)
        {
            builder.ToTable("CircuitBreakerLogs");
        }
    }
}
