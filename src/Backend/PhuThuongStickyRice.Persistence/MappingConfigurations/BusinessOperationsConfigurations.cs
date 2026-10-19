using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class CheckInConfiguration : IEntityTypeConfiguration<CheckIn>
    {
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

    public class CorporateClientConfiguration : IEntityTypeConfiguration<CorporateClient>
    {
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

    public class CustomerPhotoConfiguration : IEntityTypeConfiguration<CustomerPhoto>
    {
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

    public class WorkShiftConfiguration : IEntityTypeConfiguration<WorkShift>
    {
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

    public class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
    {
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

    public class WalletAccountConfiguration : IEntityTypeConfiguration<WalletAccount>
    {
        public void Configure(EntityTypeBuilder<WalletAccount> builder)
        {
            builder.ToTable("WalletAccounts");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Phone).HasMaxLength(WalletAccount.PhoneMaxLength).IsRequired();
            builder.HasIndex(x => x.Phone).IsUnique();
            builder.Property(x => x.Balance).HasColumnType("decimal(18,0)").IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
        }
    }

    public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
    {
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
        }
    }

    public class ShiftCloseConfiguration : IEntityTypeConfiguration<ShiftClose>
    {
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
