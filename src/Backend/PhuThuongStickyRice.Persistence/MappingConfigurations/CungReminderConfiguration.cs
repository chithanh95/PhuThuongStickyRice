using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class CungReminderConfiguration : IEntityTypeConfiguration<CungReminder>
    {
        public void Configure(EntityTypeBuilder<CungReminder> builder)
        {
            builder.ToTable("CungReminders");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.CustomerName)
                .HasMaxLength(CungReminder.NameMaxLength)
                .IsRequired();
            builder.Property(x => x.CustomerPhone)
                .HasMaxLength(CungReminder.PhoneMaxLength)
                .IsRequired();
            builder.Property(x => x.ReminderType)
                .HasMaxLength(20)
                .IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();

            builder.HasIndex(x => x.CustomerPhone);
        }
    }
}
