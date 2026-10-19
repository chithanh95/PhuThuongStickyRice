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
        /// WHERE Balance &gt;= amount). Chạy trong DbContext/UoW hiện tại nên
        /// tham gia cùng transaction với phần còn lại của nghiệp vụ.
        /// </summary>
        public async Task<bool> TryDecrementBalanceAsync(
            Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<WalletAccount>()
                .Where(x => x.Id == accountId && x.Balance >= amount)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Balance, x => x.Balance - amount)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
            return affected > 0;
        }
    }
}
