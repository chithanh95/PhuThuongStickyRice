using PhuThuongStickyRice.CrossCuttingConcerns.Locks;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Persistence.Locks;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.EntityFrameworkCore;

namespace PhuThuongStickyRice.Persistence
{
    /// <summary>
    /// ABP DbContext. Still implements the template <see cref="IUnitOfWork"/> so the
    /// legacy CommandQueryDispatcher/EntityCrudService paths keep working unchanged.
    /// </summary>
    public class StickyRiceDbContext : AbpDbContext<StickyRiceDbContext>, IUnitOfWork, IDataProtectionKeyContext
    {
        private IDbContextTransaction _dbContextTransaction;

        public StickyRiceDbContext(DbContextOptions<StickyRiceDbContext> options)
            : base(options)
        {
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        // ABP discovers entities for its default IRepository<TEntity, TKey>
        // registration from DbSet<> properties (DbContextHelper.GetEntityTypes).
        // Every domain entity needs a DbSet here, otherwise constructor
        // injection of Volo.Abp.Domain.Repositories.IRepository<,> fails at
        // runtime with an Autofac "no constructors bindable" error.
        public DbSet<AddressBook> AddressBooks { get; set; }
        public DbSet<AppLog> AppLogs { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<AuditLogEntry> AuditLogEntries { get; set; }
        public DbSet<BlacklistedPhone> BlacklistedPhones { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<CheckIn> CheckIns { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboItem> ComboItems { get; set; }
        public DbSet<CorporateClient> CorporateClients { get; set; }
        public DbSet<CustomMigrationHistory> CustomMigrationHistories { get; set; }
        public DbSet<CustomerAddress> CustomerAddresses { get; set; }
        public DbSet<CustomerReminder> CustomerReminders { get; set; }
        public DbSet<CustomerTag> CustomerTags { get; set; }
        public DbSet<DailyInventory> DailyInventories { get; set; }
        public DbSet<CustomerNote> CustomerNotes { get; set; }
        public DbSet<CustomerPhoto> CustomerPhotos { get; set; }
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<EmailMessage> EmailMessages { get; set; }
        public DbSet<FaqItem> FaqItems { get; set; }
        public DbSet<FeatureFlag> FeatureFlags { get; set; }
        public DbSet<FunnelEvent> FunnelEvents { get; set; }
        public DbSet<HolidaySchedule> HolidaySchedules { get; set; }
        public DbSet<LandingPageContent> LandingPageContents { get; set; }
        public DbSet<LoginAudit> LoginAudits { get; set; }
        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }
        public DbSet<MessageTemplate> MessageTemplates { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderLine> OrderLines { get; set; }
        public DbSet<OrderReview> OrderReviews { get; set; }
        public DbSet<OrderReviewLine> OrderReviewLines { get; set; }
        public DbSet<OrderRewardRule> OrderRewardRules { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public DbSet<OrderMessageTemplate> OrderMessageTemplates { get; set; }
        public DbSet<OutboxEvent> OutboxEvents { get; set; }
        public DbSet<PasswordHistory> PasswordHistories { get; set; }
        public DbSet<PolicyPage> PolicyPages { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductChangeLog> ProductChangeLogs { get; set; }
        public DbSet<ProductSizeOption> ProductSizeOptions { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromoSubscriber> PromoSubscribers { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }

        public DbSet<LuckySpin> LuckySpins { get; set; }

        public DbSet<Referral> Referrals { get; set; }

        public DbSet<QuickRating> QuickRatings { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<StaffChatMessage> StaffChatMessages { get; set; }

        public DbSet<ShoppingCart> ShoppingCarts { get; set; }

        public DbSet<OffPeakPromo> OffPeakPromos { get; set; }

        public DbSet<CheckInStreak> CheckInStreaks { get; set; }

        public DbSet<DishPoll> DishPolls { get; set; }

        public DbSet<FavoriteDish> FavoriteDishes { get; set; }

        public DbSet<DishVote> DishVotes { get; set; }
        public DbSet<QuoteRequest> QuoteRequests { get; set; }
        public DbSet<CungReminder> CungReminders { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RoleClaim> RoleClaims { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ShiftClose> ShiftCloses { get; set; }
        public DbSet<Shipper> Shippers { get; set; }
        public DbSet<StaffActivityLog> StaffActivityLogs { get; set; }
        public DbSet<ShopSetting> ShopSettings { get; set; }
        public DbSet<ShortLink> ShortLinks { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<TableQr> TableQrs { get; set; }
        public DbSet<Topping> Toppings { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserClaim> UserClaims { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }
        public DbSet<WalletAccount> WalletAccounts { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<WebhookDelivery> WebhookDeliveries { get; set; }
        public DbSet<WebhookEndpoint> WebhookEndpoints { get; set; }
        public DbSet<WorkShift> WorkShifts { get; set; }

        /// <summary>
        /// Mở transaction DB mới; giữ reference vào field để
        /// <see cref="CommitTransactionAsync"/> commit cùng transaction này
        /// (legacy template IUnitOfWork API).
        /// </summary>
        public async Task<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
        {
            _dbContextTransaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return _dbContextTransaction;
        }

        /// <summary>
        /// Mở transaction DB và giữ thêm distributed lock (sp_getapplock theo
        /// transaction) tên <paramref name="lockName"/>: dùng khi 1 nghiệp vụ
        /// cần khóa phạm vi rộng vượt qua 1 row của 1 bảng. Ném
        /// <see cref="CouldNotAcquireLockException"/> nếu không lấy được lock
        /// — caller không được tiếp tục nghiệp vụ khi không có lock.
        /// </summary>
        public async Task<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, string lockName = null, CancellationToken cancellationToken = default)
        {
            _dbContextTransaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);

            var sqlLock = new SqlDistributedLock(_dbContextTransaction.GetDbTransaction() as SqlTransaction);
            var lockScope = sqlLock.Acquire(lockName);
            if (lockScope == null)
            {
                // CA2201: ném exception chuyên dụng thay vì Exception chung chung —
                // nhất quán với SqlServerDistributedLockProvider dùng
                // CouldNotAcquireLockException cho cùng tình huống.
                throw new CouldNotAcquireLockException($"Could not acquire lock: {lockName}");
            }

            return _dbContextTransaction;
        }

        /// <summary>
        /// Commit transaction đã mở bởi <c>BeginTransactionAsync</c>.
        /// </summary>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            await _dbContextTransaction.CommitAsync(cancellationToken);
        }

        /// <summary>
        /// Đóng dấu thời gian tập trung cho mọi entity <see cref="ITrackable"/>
        /// ngay tại thời điểm lưu: bản ghi mới (Added) chưa có
        /// <c>CreatedDateTime</c> thì gán thời gian hiện tại; bản ghi sửa
        /// (Modified) luôn gán <c>UpdatedDateTime</c>. Đây là chốt chặn duy
        /// nhất phủ mọi đường CRUD — trước đây chỉ Repository tự chế
        /// (<c>Repositories/Repository.cs</c>) mới stamp, còn các luồng dùng
        /// repository ABP mặc định hoặc factory/initializer (vd tạo đơn
        /// hàng) thì <c>CreatedDateTime</c> xuống DB thành 0001-01-01
        /// (user phát hiện 2026-10-21 qua bảng Orders; rà soát model cho
        /// thấy 80 bảng có cột ngày giờ dùng chung cơ chế này). Giá trị đã
        /// được gán tường minh (seed dữ liệu cũ, event handler, Repository
        /// tự chế) được giữ nguyên nhờ điều kiện chỉ stamp khi đang default.
        /// </summary>
        private void StampTrackableTimestamps()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var entry in ChangeTracker.Entries<ITrackable>())
            {
                if (entry.State == EntityState.Added
                    && entry.Entity.CreatedDateTime == default)
                {
                    entry.Entity.CreatedDateTime = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDateTime = now;
                }
            }
        }

        /// <summary>Stamp ngày giờ ITrackable rồi lưu (đường bất đồng bộ — ABP UoW dùng đường này).</summary>
        /// <param name="cancellationToken">Token hủy.</param>
        /// <returns>Số bản ghi đã ghi xuống DB.</returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            StampTrackableTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>Stamp ngày giờ ITrackable rồi lưu (đường đồng bộ — seeder/tool dùng đường này).</summary>
        /// <returns>Số bản ghi đã ghi xuống DB.</returns>
        public override int SaveChanges()
        {
            StampTrackableTimestamps();
            return base.SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
