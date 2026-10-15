using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
    {
        public void Configure(EntityTypeBuilder<CustomerAddress> builder)
        {
            builder.ToTable("CustomerAddresses");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.UserId).IsRequired();
            builder.HasIndex(x => x.UserId);

            builder.Property(x => x.Label).HasMaxLength(CustomerAddress.LabelMaxLength);
            builder.Property(x => x.ReceiverName).HasMaxLength(CustomerAddress.ReceiverNameMaxLength).IsRequired();
            builder.Property(x => x.ReceiverPhone).HasMaxLength(CustomerAddress.PhoneMaxLength).IsRequired();
            builder.Property(x => x.AddressDetail).HasMaxLength(CustomerAddress.AddressMaxLength).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(CustomerAddress.NoteMaxLength);
        }
    }
}
