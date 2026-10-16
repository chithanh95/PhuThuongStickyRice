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
    /// legacy Dispatcher/CrudService paths keep working unchanged.
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
        public DbSet<GroupOrder> GroupOrders { get; set; }
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

        public DbSet<OffPeakPromo> OffPeakPromos { get; set; }

        public DbSet<CheckInStreak> CheckInStreaks { get; set; }

        public DbSet<DishPoll> DishPolls { get; set; }

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

        public async Task<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
        {
            _dbContextTransaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return _dbContextTransaction;
        }

        public async Task<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, string lockName = null, CancellationToken cancellationToken = default)
        {
            _dbContextTransaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);

            var sqlLock = new SqlDistributedLock(_dbContextTransaction.GetDbTransaction() as SqlTransaction);
            var lockScope = sqlLock.Acquire(lockName);
            if (lockScope == null)
            {
                throw new Exception($"Could not acquire lock: {lockName}");
            }

            return _dbContextTransaction;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            await _dbContextTransaction.CommitAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
