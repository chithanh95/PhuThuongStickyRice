using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    /// <summary>
    /// Cấu hình ánh xạ bảng <c>FeatureFlags</c> — cờ bật/tắt tính năng
    /// (feature toggle), cho phép admin bật/tắt chức năng mà không cần
    /// deploy lại code. Unique index Key: mỗi cờ chỉ tồn tại 1 bản ghi.
    /// </summary>
    public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
    {
        /// <summary>Khai báo tên bảng, độ dài cột và index tra cứu cờ theo Key.</summary>
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

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>Branches</c> — danh sách chi nhánh/cơ sở bán
    /// hàng của quán (địa chỉ, SĐT, trạng thái hoạt động).
    /// </summary>
    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        /// <summary>Khai báo tên bảng và độ dài các cột thông tin chi nhánh.</summary>
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

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>WebhookEndpoints</c> — các URL đối tác đăng ký
    /// nhận sự kiện (đơn mới, thanh toán...). Cột Secret dùng để ký chữ ký
    /// xác thực payload, tránh endpoint giả mạo nhận sự kiện.
    /// </summary>
    public class WebhookEndpointConfiguration : IEntityTypeConfiguration<WebhookEndpoint>
    {
        /// <summary>Khai báo tên bảng và độ dài các cột cấu hình endpoint webhook.</summary>
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

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>WebhookDeliveries</c> — nhật ký từng lần đẩy
    /// sự kiện tới endpoint (thành công/thất bại + thông báo lỗi). Index At:
    /// tra cứu lịch sử gửi theo thời gian khi đối soát với đối tác.
    /// </summary>
    public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
    {
        /// <summary>Khai báo tên bảng, độ dài cột và index tra cứu log theo thời gian.</summary>
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

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>ShortLinks</c> — link rút gọn (mã ngắn ánh xạ
    /// tới URL đích, dùng cho SMS/Zalo giới hạn ký tự). Unique index Code:
    /// mã rút gọn không được trùng để điều hướng đúng đích.
    /// </summary>
    public class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
    {
        /// <summary>Khai báo tên bảng, độ dài cột và index tra cứu mã rút gọn.</summary>
        public void Configure(EntityTypeBuilder<ShortLink> builder)
        {
            builder.ToTable("ShortLinks");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Code).HasMaxLength(ShortLink.CodeMaxLength).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.TargetUrl).HasMaxLength(ShortLink.TargetUrlMaxLength).IsRequired();
        }
    }

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>AddressBook</c> — sổ địa chỉ đã lưu của khách
    /// (nhãn Nhà/Công ty...). Index Phone: lấy nhanh danh sách địa chỉ theo
    /// SĐT khi khách đặt hàng, không phải nhập lại.
    /// </summary>
    public class AddressBookConfiguration : IEntityTypeConfiguration<AddressBook>
    {
        /// <summary>Khai báo tên bảng, độ dài cột và index tra cứu địa chỉ theo SĐT.</summary>
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

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>AppLogs</c> — nhật ký log ứng dụng lưu trong
    /// DB (mức độ, nguồn, nội dung). Index At/Level: lọc log theo thời gian
    /// và mức độ khi tra cứu sự cố.
    /// </summary>
    public class AppLogConfiguration : IEntityTypeConfiguration<AppLog>
    {
        /// <summary>Khai báo tên bảng, độ dài cột và index lọc log theo thời gian/mức độ.</summary>
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
