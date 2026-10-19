using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    /// <summary>
    /// Cấu hình bảng giỏ hàng server-side (item 137: mỗi user 1 bản ghi).
    /// </summary>
    public class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
    {
        public void Configure(EntityTypeBuilder<ShoppingCart> builder)
        {
            builder.ToTable("ShoppingCarts");

            builder.Property(x => x.UserId).HasMaxLength(100);

            // perf-hunt 2026-10-19: DB đã có IX_ShoppingCarts_UserId (unique,
            // filtered) từ migration 20261019070000_AddShoppingCart nhưng thiếu
            // config class → model tưởng UserId là nvarchar(max) không index.
            // Khai báo khớp DB để model đồng bộ (không cần migration mới);
            // đồng thời tránh scaffold sau này sinh DROP index + AlterColumn.
            builder.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        }
    }
}
