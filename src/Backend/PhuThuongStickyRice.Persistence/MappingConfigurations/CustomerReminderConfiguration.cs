using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class CustomerReminderConfiguration : IEntityTypeConfiguration<CustomerReminder>
    {
        public void Configure(EntityTypeBuilder<CustomerReminder> builder)
        {
            builder.ToTable("CustomerReminders");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.CustomerPhone).HasMaxLength(CustomerReminder.PhoneMaxLength).IsRequired();
            builder.HasIndex(x => x.CustomerPhone);

            builder.Property(x => x.CustomerName).HasMaxLength(CustomerReminder.NameMaxLength);
            builder.Property(x => x.Message).HasMaxLength(CustomerReminder.MessageMaxLength).IsRequired();
            builder.Property(x => x.SentVia).HasMaxLength(CustomerReminder.SentViaMaxLength);
            builder.Property(x => x.CreatedBy).HasMaxLength(CustomerReminder.CreatedByMaxLength);
        }
    }
}
