using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class FunnelEventConfiguration : IEntityTypeConfiguration<FunnelEvent>
    {
        public void Configure(EntityTypeBuilder<FunnelEvent> builder)
        {
            builder.ToTable("FunnelEvents");

            // perf-hunt 2026-10-19: the table grows one row per page view
            // (unbounded). The admin funnel report filters on CreatedDateTime,
            // so this index avoids a full table scan.
            builder.HasIndex(x => x.CreatedDateTime);
        }
    }
}
