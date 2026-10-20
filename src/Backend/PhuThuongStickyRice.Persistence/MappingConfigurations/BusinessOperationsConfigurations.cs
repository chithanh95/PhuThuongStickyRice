using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    /// <summary>
    /// Cấu hình ánh xạ bảng <c>CheckIns</c> — lịch sử điểm danh nhận điểm
    /// thưởng hàng ngày của khách. Unique index (Phone, Date): mỗi SĐT chỉ
    /// điểm danh được 1 lần/ngày, chống ghi trùng khi khách bấm nhanh hoặc
    /// API bị gọi lặp.
    /// </summary>
    public class CheckInConfiguration : IEntityTypeConfiguration<CheckIn>
    {
        /// <summary>Khai báo tên bảng, độ dài cột và index chống điểm danh trùng ngày.</summary>
        public void Configure(EntityTypeBuilder<CheckIn> builder)
        {
            builder.ToTable("CheckIns");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Phone).HasMaxLength(CheckIn.PhoneMaxLength).IsRequired();
            builder.Property(x => x.Date).IsRequired();
            builder.Property(x => x.PointsAwarded).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => new { x.Phone, x.Date }).IsUnique();
        }
    }

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>CorporateClients</c> — khách hàng doanh nghiệp
    /// đặt xôi theo lịch cố định (đặt cung giờ, giao vào ngày trong tuần).
    /// Index Weekday: lọc nhanh danh sách khách cần giao theo thứ khi lập
    /// lịch giao hàng.
    /// </summary>
    public class CorporateClientConfiguration : IEntityTypeConfiguration<CorporateClient>
    {
        /// <summary>Khai báo tên bảng, độ dài cột và index tra cứu theo ngày trong tuần.</summary>
        public void Configure(EntityTypeBuilder<CorporateClient> builder)
        {
            builder.ToTable("CorporateClients");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.CompanyName).HasMaxLength(CorporateClient.NameMaxLength).IsRequired();
            builder.Property(x => x.ContactName).HasMaxLength(CorporateClient.NameMaxLength);
            builder.Property(x => x.Phone).HasMaxLength(CorporateClient.PhoneMaxLength).IsRequired();
            builder.Property(x => x.Address).HasMaxLength(CorporateClient.AddressMaxLength);
            builder.Property(x => x.Weekday).IsRequired();
            builder.Property(x => x.ItemsJson).HasMaxLength(4000);
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.HasIndex(x => x.Weekday);
        }
    }

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>CustomerPhotos</c> — ảnh do khách tải lên
    /// (UGC) cho gallery cộng đồng. Chỉ ảnh có IsApproved = true mới hiện ở
    /// gallery công khai; composite index bên dưới phục vụ đúng truy vấn này
    /// (xem comment perf-hunt trong thân method).
    /// </summary>
    public class CustomerPhotoConfiguration : IEntityTypeConfiguration<CustomerPhoto>
    {
        /// <summary>Khai báo tên bảng, độ dài cột và composite index cho gallery đã duyệt.</summary>
        public void Configure(EntityTypeBuilder<CustomerPhoto> builder)
        {
            builder.ToTable("CustomerPhotos");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Caption).HasMaxLength(CustomerPhoto.CaptionMaxLength);
            builder.Property(x => x.Phone).HasMaxLength(CustomerPhoto.PhoneMaxLength);
            builder.Property(x => x.Name).HasMaxLength(CustomerPhoto.NameMaxLength);
            builder.Property(x => x.IsApproved).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            // perf-hunt 2026-10-19 (run 14:15): public gallery (cache-miss) và
            // admin list đều WHERE IsApproved + ORDER BY CreatedAt DESC;
            // UGC tăng vô hạn. Composite thay index đơn IsApproved: equality
            // trên IsApproved rồi sort ngay trên index. Non-unique, lookup-only.
            builder.HasIndex(x => new { x.IsApproved, x.CreatedAt })
                .HasDatabaseName("IX_CustomerPhotos_IsApproved_CreatedAt");
        }
    }

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>WorkShifts</c> — ca làm việc của nhân viên
    /// (giờ bắt đầu/kết thúc theo từng ngày). Index Date: tra cứu nhanh các
    /// ca trong một ngày khi chấm công/lập lịch.
    /// </summary>
    public class WorkShiftConfiguration : IEntityTypeConfiguration<WorkShift>
    {
        /// <summary>Khai báo tên bảng, độ dài cột và index tra cứu theo ngày.</summary>
        public void Configure(EntityTypeBuilder<WorkShift> builder)
        {
            builder.ToTable("WorkShifts");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.StaffName).HasMaxLength(WorkShift.StaffNameMaxLength).IsRequired();
            builder.Property(x => x.Date).IsRequired();
            builder.Property(x => x.StartTime).IsRequired();
            builder.Property(x => x.EndTime).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(WorkShift.NoteMaxLength);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => x.Date);
        }
    }

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>MessageTemplates</c> — mẫu nội dung tin nhắn
    /// gửi cho khách qua các kênh (Zalo/Telegram/Email/SMS), admin soạn sẵn
    /// một lần rồi tái sử dụng khi gửi hàng loạt.
    /// </summary>
    public class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
    {
        /// <summary>Khai báo tên bảng và độ dài các cột nội dung mẫu tin nhắn.</summary>
        public void Configure(EntityTypeBuilder<MessageTemplate> builder)
        {
            builder.ToTable("MessageTemplates");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Name).HasMaxLength(MessageTemplate.NameMaxLength).IsRequired();
            builder.Property(x => x.Channel).HasMaxLength(MessageTemplate.ChannelMaxLength).IsRequired();
            builder.Property(x => x.Body).HasMaxLength(MessageTemplate.BodyMaxLength).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
        }
    }

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>WalletAccounts</c> — tài khoản ví tiền của
    /// khách (nạp tiền, trừ khi mua hàng, hoàn tiền). Unique index Phone:
    /// mỗi SĐT chỉ có đúng 1 ví, tránh tạo trùng ví khi nạp tiền đồng thời.
    /// </summary>
    public class WalletAccountConfiguration : IEntityTypeConfiguration<WalletAccount>
    {
        /// <summary>Khai báo tên bảng, kiểu decimal cho số dư và các index tra cứu ví.</summary>
        public void Configure(EntityTypeBuilder<WalletAccount> builder)
        {
            builder.ToTable("WalletAccounts");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Phone).HasMaxLength(WalletAccount.PhoneMaxLength).IsRequired();
            builder.HasIndex(x => x.Phone).IsUnique();
            builder.Property(x => x.Balance).HasColumnType("decimal(18,0)").IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();

            // perf-hunt 2026-10-19 (run 14:46): WalletAppService.GetListAsync
            // chạy OrderByDescending(Balance) + Take(200) mỗi lần admin mở
            // trang ví tiền; bảng tăng theo mỗi khách. Index non-unique,
            // lookup-only, không đổi nghiệp vụ.
            builder.HasIndex(x => x.Balance);
        }
    }

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>WalletTransactions</c> — lịch sử biến động ví
    /// (nạp, trừ khi mua, hoàn tiền...). Composite index (Phone, CreatedAt)
    /// phục vụ tra cứu lịch sử theo SĐT (xem comment perf-hunt); unique index
    /// IdempotencyKey là khóa chống hoàn tiền trùng (xem comment bug-hunt).
    /// </summary>
    public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
    {
        /// <summary>Khai báo tên bảng, kiểu decimal cho số tiền và các index chống trùng/tra cứu.</summary>
        public void Configure(EntityTypeBuilder<WalletTransaction> builder)
        {
            builder.ToTable("WalletTransactions");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Phone).HasMaxLength(WalletTransaction.PhoneMaxLength).IsRequired();
            builder.Property(x => x.Amount).HasColumnType("decimal(18,0)").IsRequired();
            builder.Property(x => x.Reason).HasMaxLength(WalletTransaction.ReasonMaxLength).IsRequired();
            builder.Property(x => x.BalanceAfter).HasColumnType("decimal(18,0)").IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(200);
            builder.Property(x => x.CreatedAt).IsRequired();
            // perf-hunt 2026-10-19 (run 14:05): GetTransactionsAsync (admin tra
            // cứu ví theo SĐT) lọc Phone + sort CreatedAt desc + Take(100) mỗi lần
            // tra cứu; bảng WalletTransactions tăng vô hạn (không bao giờ xóa).
            // Composite (Phone, CreatedAt): seek equality trên Phone rồi sort ngay
            // trên index, thay index đơn IX_WalletTransactions_Phone (redundant).
            builder.HasIndex(x => new { x.Phone, x.CreatedAt });
            // bug-hunt 2026-10-19 (run 15:35): khóa chống hoàn tiền ví trùng.
            // RefundAsync insert dòng guard với IdempotencyKey trước khi cộng
            // tiền; unique index này làm 2 lần hủy đơn đồng thời (khách + staff,
            // webhook gọi lặp) chỉ có đúng 1 bên insert được — bên thua return
            // mà không chạm số dư. NULL không va chạm nên các dòng charge/nạp/
            // trừ tay (không đặt khóa) không bị ảnh hưởng.
            builder.Property(x => x.IdempotencyKey)
                .HasMaxLength(WalletTransaction.IdempotencyKeyMaxLength);
            builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        }
    }

    /// <summary>
    /// Cấu hình ánh xạ bảng <c>ShiftCloses</c> — biên bản chốt ca: đối soát
    /// tiền COD thực tế thu được với số dự kiến cuối ngày/ca. Unique index
    /// Date: mỗi ngày chỉ được chốt 1 lần, tránh chốt trùng làm lệch số
    /// liệu đối soát.
    /// </summary>
    public class ShiftCloseConfiguration : IEntityTypeConfiguration<ShiftClose>
    {
        /// <summary>Khai báo tên bảng, kiểu decimal cho tiền COD và index chống chốt trùng ngày.</summary>
        public void Configure(EntityTypeBuilder<ShiftClose> builder)
        {
            builder.ToTable("ShiftCloses");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Date).IsRequired();
            builder.HasIndex(x => x.Date).IsUnique();
            builder.Property(x => x.ExpectedCod).HasColumnType("decimal(18,0)").IsRequired();
            builder.Property(x => x.ActualCod).HasColumnType("decimal(18,0)").IsRequired();
            builder.Property(x => x.Note).HasMaxLength(ShiftClose.NoteMaxLength);
            builder.Property(x => x.CreatedBy).HasMaxLength(200);
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
