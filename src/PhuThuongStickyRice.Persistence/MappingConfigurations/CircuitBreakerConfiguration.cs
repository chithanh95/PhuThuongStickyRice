﻿using PhuThuongStickyRice.Persistence.CircuitBreakers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
