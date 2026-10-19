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
    public class WalletTransactionRepository
        : EfCoreRepository<StickyRiceDbContext, WalletTransaction, System.Guid>, IWalletTransactionRepository
    {
        public WalletTransactionRepository(IDbContextProvider<StickyRiceDbContext> dbContextProvider,
            Volo.Abp.DependencyInjection.IAbpLazyServiceProvider lazyServiceProvider)
            : base(dbContextProvider)
        {
            // See ProductRepository: ABP property injection needs Autofac; set explicitly.
            LazyServiceProvider = lazyServiceProvider;
        }

        /// <summary>
        /// Chèn dòng guard hoàn tiền nguyên tử (xem <see cref="IWalletTransactionRepository.TryInsertRefundGuardAsync"/>).
        /// Chạy trong DbContext/UoW hiện tại nên tham gia cùng transaction với
        /// phần còn lại của nghiệp vụ hủy đơn — rollback sẽ cuốn cả dòng guard.
        /// </summary>
        public async Task<WalletTransaction> TryInsertRefundGuardAsync(
            WalletTransaction guard, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            if (guard.Id == Guid.Empty)
            {
                // Không qua ABP InsertAsync nên tự cấp Id (config có
                // newsequentialid() nhưng SQLite test không hỗ trợ).
                guard.Id = Guid.NewGuid();
            }

            // Đường nhanh: đã có dòng guard -> bên này thua, không insert.
            var exists = await dbContext.Set<WalletTransaction>()
                .AnyAsync(x => x.IdempotencyKey == guard.IdempotencyKey, cancellationToken);
            if (exists)
            {
                return null;
            }

            try
            {
                await dbContext.Set<WalletTransaction>().AddAsync(guard, cancellationToken);
                // Flush ngay (tương đương autoSave: true của ABP InsertAsync)
                // để unique index phát huy tác dụng trong race.
                await dbContext.SaveChangesAsync(cancellationToken);
                return guard;
            }
            catch (DbUpdateException)
            {
                // Race: 2 request cùng qua pre-check, unique index chỉ cho 1
                // bên insert. Detach entity lỗi — nếu không, SaveChanges ở
                // cuối UoW sẽ thử insert lại và ném lỗi lần nữa, làm vỡ cả
                // luồng hủy đơn của bên thua (vốn đã được bên thắng hoàn tiền).
                dbContext.Entry(guard).State = EntityState.Detached;
                var winnerExists = await dbContext.Set<WalletTransaction>()
                    .AnyAsync(x => x.IdempotencyKey == guard.IdempotencyKey, cancellationToken);
                // Bên thắng đã ghi guard: thua là đúng, return null.
                // Không thấy dòng nào: lỗi thật (không phải duplicate) -> ném lại.
                if (winnerExists)
                {
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        /// Cập nhật BalanceAfter của dòng guard (xem <see cref="IWalletTransactionRepository.UpdateRefundBalanceAfterAsync"/>).
        /// Dùng ExecuteUpdateAsync để không phụ thuộc change tracker.
        /// </summary>
        public async Task UpdateRefundBalanceAfterAsync(
            string idempotencyKey, decimal balanceAfter, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            await dbContext.Set<WalletTransaction>()
                .Where(x => x.IdempotencyKey == idempotencyKey)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.BalanceAfter, balanceAfter),
                    cancellationToken);
        }
    }
}
