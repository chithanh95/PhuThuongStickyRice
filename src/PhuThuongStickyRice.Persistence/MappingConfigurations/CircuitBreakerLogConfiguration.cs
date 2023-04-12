using PhuThuongStickyRice.Persistence.CircuitBreakers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
