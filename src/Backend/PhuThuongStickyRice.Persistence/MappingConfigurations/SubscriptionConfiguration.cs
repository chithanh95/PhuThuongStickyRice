using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.ToTable("Subscriptions");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Phone)
                .HasMaxLength(Subscription.PhoneMaxLength)
                .IsRequired();
            builder.Property(x => x.CustomerName)
                .HasMaxLength(Subscription.NameMaxLength)
                .IsRequired();
            builder.Property(x => x.ProductId).IsRequired();
            builder.Property(x => x.ProductName)
                .HasMaxLength(Subscription.ProductNameMaxLength)
                .IsRequired();
            builder.Property(x => x.SizeName).HasMaxLength(Subscription.SizeNameMaxLength);
            builder.Property(x => x.ToppingNames).HasMaxLength(Subscription.ToppingNamesMaxLength);
            builder.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.DeliveryAddress)
                .HasMaxLength(Subscription.AddressMaxLength)
                .IsRequired();
            builder.Property(x => x.StartDate).IsRequired();
            builder.Property(x => x.Active).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(Subscription.NotesMaxLength);
            builder.Property(x => x.CreatedAt).IsRequired();

            // perf-hunt 2026-10-19 (run 14:15): GetListAsync sort
            // OrderByDescending(CreatedAt) + Take(200) mỗi lần admin mở trang;
            // bảng gói đăng ký tăng dần. Index non-unique, lookup-only,
            // không đổi nghiệp vụ.
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
