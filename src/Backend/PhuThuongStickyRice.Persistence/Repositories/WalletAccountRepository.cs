#nullable enable annotations // Task<WalletAccount?> ở dưới cần nullable annotations context (CS8632)
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class WalletAccountRepository
        : EfCoreRepository<StickyRiceDbContext, WalletAccount, System.Guid>, IWalletAccountRepository
    {
        public WalletAccountRepository(IDbContextProvider<StickyRiceDbContext> dbContextProvider,
            Volo.Abp.DependencyInjection.IAbpLazyServiceProvider lazyServiceProvider)
            : base(dbContextProvider)
        {
            // See ProductRepository: ABP property injection needs Autofac; set explicitly.
            LazyServiceProvider = lazyServiceProvider;
        }

        /// <summary>
        /// Trừ số dư nguyên tử bằng ExecuteUpdateAsync (1 câu UPDATE ...
        /// WHERE Balance &gt;= amount), rồi đọc lại số dư mới (AsNoTracking
        /// để không dính entity cũ trong change tracker). Trả về số dư mới
        /// để caller ghi đúng BalanceAfter vào lịch sử giao dịch; null khi
        /// số dư không đủ (không trừ).
        /// </summary>
        public async Task<decimal?> TryDecrementBalanceAsync(
            Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<WalletAccount>()
                .Where(x => x.Id == accountId && x.Balance >= amount)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Balance, x => x.Balance - amount)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
            if (affected == 0)
            {
                return null;
            }
            return await dbContext.Set<WalletAccount>()
                .AsNoTracking()
                .Where(x => x.Id == accountId)
                .Select(x => x.Balance)
                .FirstAsync(cancellationToken);
        }

        /// <summary>
        /// Cộng số dư nguyên tử bằng ExecuteUpdateAsync (1 câu UPDATE
        /// Balance = Balance + amount), rồi đọc lại số dư mới (AsNoTracking
        /// để không dính entity cũ trong change tracker). Trả về số dư mới
        /// để caller ghi đúng BalanceAfter vào lịch sử giao dịch.
        /// </summary>
        public async Task<decimal> IncrementBalanceAsync(
            Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            await dbContext.Set<WalletAccount>()
                .Where(x => x.Id == accountId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Balance, x => x.Balance + amount)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
            return await dbContext.Set<WalletAccount>()
                .AsNoTracking()
                .Where(x => x.Id == accountId)
                .Select(x => x.Balance)
                .FirstAsync(cancellationToken);
        }

        /// <summary>
        /// Tạo ví mới nguyên tử (xem <see cref="IWalletAccountRepository.TryInsertAsync"/>).
        /// Check-then-insert có race: 2 request cùng tạo ví 1 SĐT cùng lúc đều
        /// qua được kiểm tra <c>AnyAsync</c>, request thua sẽ vi phạm unique
        /// constraint ở <c>SaveChangesAsync</c> — catch bên dưới phân biệt: nếu
        /// ví đã tồn tại thật (request kia thắng) thì trả về null như chưa bao
        /// giờ tạo, chỉ ném lại khi nguyên nhân là lỗi khác.
        /// </summary>
        public async Task<WalletAccount?> TryInsertAsync(
            WalletAccount account, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            if (account.Id == Guid.Empty)
            {
                account.Id = Guid.NewGuid();
            }

            var exists = await dbContext.Set<WalletAccount>()
                .AnyAsync(x => x.Phone == account.Phone, cancellationToken);
            if (exists)
            {
                return null;
            }

            try
            {
                await dbContext.Set<WalletAccount>().AddAsync(account, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                return account;
            }
            catch (DbUpdateException)
            {
                // Race ở trên: request khác đã insert cùng SĐT trước — detach
                // entity thất bại khỏi tracker (không dùng nữa) rồi xác nhận
                // lại; chỉ ném tiếp khi lỗi không phải do trùng ví.
                dbContext.Entry(account).State = EntityState.Detached;
                var winnerExists = await dbContext.Set<WalletAccount>()
                    .AnyAsync(x => x.Phone == account.Phone, cancellationToken);
                if (winnerExists)
                {
                    return null;
                }
                throw;
            }
        }
    }
}
