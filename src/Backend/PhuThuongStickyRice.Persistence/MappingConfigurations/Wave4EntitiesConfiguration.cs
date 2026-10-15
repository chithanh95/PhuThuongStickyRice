using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
    {
        public void Configure(EntityTypeBuilder<FeatureFlag> builder)
        {
            builder.ToTable("FeatureFlags");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Key).HasMaxLength(FeatureFlag.KeyMaxLength).IsRequired();
            builder.HasIndex(x => x.Key).IsUnique();

            builder.Property(x => x.Name).HasMaxLength(FeatureFlag.NameMaxLength).IsRequired();
            builder.Property(x => x.IsEnabled).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(FeatureFlag.DescriptionMaxLength);
        }
    }

    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.ToTable("Branches");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Name).HasMaxLength(Branch.NameMaxLength).IsRequired();
            builder.Property(x => x.Address).HasMaxLength(Branch.AddressMaxLength);
            builder.Property(x => x.Phone).HasMaxLength(Branch.PhoneMaxLength);
            builder.Property(x => x.IsActive).IsRequired();
        }
    }

    public class WebhookEndpointConfiguration : IEntityTypeConfiguration<WebhookEndpoint>
    {
        public void Configure(EntityTypeBuilder<WebhookEndpoint> builder)
        {
            builder.ToTable("WebhookEndpoints");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Name).HasMaxLength(WebhookEndpoint.NameMaxLength).IsRequired();
            builder.Property(x => x.Url).HasMaxLength(WebhookEndpoint.UrlMaxLength).IsRequired();
            builder.Property(x => x.Secret).HasMaxLength(WebhookEndpoint.SecretMaxLength);
            builder.Property(x => x.Events).HasMaxLength(WebhookEndpoint.EventsMaxLength);
            builder.Property(x => x.IsActive).IsRequired();
        }
    }

    public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
    {
        public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
        {
            builder.ToTable("WebhookDeliveries");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.At).IsRequired();
            builder.Property(x => x.EndpointId).IsRequired();
            builder.Property(x => x.EndpointName).HasMaxLength(WebhookEndpoint.NameMaxLength);
            builder.Property(x => x.Event).HasMaxLength(WebhookDelivery.EventMaxLength);
            builder.Property(x => x.Success).IsRequired();
            builder.Property(x => x.Error).HasMaxLength(2000);
            builder.HasIndex(x => x.At);
        }
    }

    public class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
    {
        public void Configure(EntityTypeBuilder<ShortLink> builder)
        {
            builder.ToTable("ShortLinks");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Code).HasMaxLength(ShortLink.CodeMaxLength).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.TargetUrl).HasMaxLength(ShortLink.TargetUrlMaxLength).IsRequired();
        }
    }

    public class GroupOrderConfiguration : IEntityTypeConfiguration<GroupOrder>
    {
        public void Configure(EntityTypeBuilder<GroupOrder> builder)
        {
            builder.ToTable("GroupOrders");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Code).HasMaxLength(GroupOrder.CodeMaxLength).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.CreatorPhone).HasMaxLength(GroupOrder.PhoneMaxLength);
            builder.Property(x => x.GroupName).HasMaxLength(100);
            builder.Property(x => x.Status).HasMaxLength(GroupOrder.StatusMaxLength).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ResultOrderNumber).HasMaxLength(50);
        }
    }

    public class AddressBookConfiguration : IEntityTypeConfiguration<AddressBook>
    {
        public void Configure(EntityTypeBuilder<AddressBook> builder)
        {
            builder.ToTable("AddressBook");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Phone).HasMaxLength(AddressBook.PhoneMaxLength).IsRequired();
            builder.Property(x => x.Label).HasMaxLength(AddressBook.LabelMaxLength).IsRequired();
            builder.Property(x => x.Address).HasMaxLength(AddressBook.AddressMaxLength).IsRequired();
            builder.HasIndex(x => x.Phone);
        }
    }

    public class AppLogConfiguration : IEntityTypeConfiguration<AppLog>
    {
        public void Configure(EntityTypeBuilder<AppLog> builder)
        {
            builder.ToTable("AppLogs");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.At).IsRequired();
            builder.Property(x => x.Level).HasMaxLength(AppLog.LevelMaxLength).IsRequired();
            builder.Property(x => x.Source).HasMaxLength(AppLog.SourceMaxLength);
            builder.Property(x => x.Message).HasMaxLength(AppLog.MessageMaxLength);
            builder.HasIndex(x => x.At);
            builder.HasIndex(x => x.Level);
        }
    }
}
