using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class DailyInventoryConfiguration : IEntityTypeConfiguration<DailyInventory>
    {
        public void Configure(EntityTypeBuilder<DailyInventory> builder)
        {
            builder.ToTable("DailyInventories");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Date).IsRequired();
            builder.Property(x => x.ProductId).IsRequired();
            builder.Property(x => x.ProductName)
                .HasMaxLength(DailyInventory.ProductNameMaxLength)
                .IsRequired();
            builder.Property(x => x.RemainingQty).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(DailyInventory.NoteMaxLength);
            builder.Property(x => x.CreatedBy).HasMaxLength(DailyInventory.CreatedByMaxLength);
            builder.Property(x => x.CreatedAt).IsRequired();

            builder.HasIndex(x => new { x.Date, x.ProductId }).IsUnique();

            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
