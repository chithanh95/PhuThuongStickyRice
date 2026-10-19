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
    /// <summary>
    /// Repository tài khoản tích điểm (loyalty) theo SĐT: cộng/trừ/hoàn điểm
    /// đều là UPDATE nguyên tử (ExecuteUpdateAsync) để chống race khi 2 request
    /// cùng chạm vào 1 tài khoản — trừ điểm chỉ thành công khi số dư đủ
    /// (WHERE Points &gt;= points), không bao giờ âm điểm.
    /// </summary>
    public class LoyaltyAccountRepository
        : EfCoreRepository<StickyRiceDbContext, LoyaltyAccount, System.Guid>, ILoyaltyAccountRepository
    {
        public LoyaltyAccountRepository(IDbContextProvider<StickyRiceDbContext> dbContextProvider,
            Volo.Abp.DependencyInjection.IAbpLazyServiceProvider lazyServiceProvider)
            : base(dbContextProvider)
        {
            // See ProductRepository: ABP property injection needs Autofac; set explicitly.
            LazyServiceProvider = lazyServiceProvider;
        }

        /// <summary>
        /// Trừ điểm nguyên tử bằng ExecuteUpdateAsync (1 câu UPDATE ...
        /// WHERE Phone = phone AND Points &gt;= points). Chạy trong
        /// DbContext/UoW hiện tại nên tham gia cùng transaction với phần còn
        /// lại của nghiệp vụ.
        /// </summary>
        public async Task<bool> TryDecrementPointsAsync(
            string phone, int points, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<LoyaltyAccount>()
                .Where(x => x.Phone == phone && x.Points >= points)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Points, x => x.Points - points)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
            return affected > 0;
        }

        /// <summary>
        /// Cộng điểm nguyên tử bằng ExecuteUpdateAsync (1 câu UPDATE ...
        /// SET Points = Points + points, TotalEarned = TotalEarned + points
        /// WHERE Phone = phone). Chạy trong DbContext/UoW hiện tại nên tham
        /// gia cùng transaction với phần còn lại của nghiệp vụ.
        /// </summary>
        public async Task<bool> TryIncrementPointsAsync(
            string phone, int points, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<LoyaltyAccount>()
                .Where(x => x.Phone == phone)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Points, x => x.Points + points)
                    .SetProperty(x => x.TotalEarned, x => x.TotalEarned + points)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
            return affected > 0;
        }

        /// <summary>
        /// Hoàn điểm nguyên tử: chỉ cộng Points, không cộng TotalEarned —
        /// hoàn điểm hủy đơn là trả lại số dư, không phải kiếm mới (xem
        /// interface). Chạy trong UoW hiện tại nên tham gia cùng transaction.
        /// </summary>
        public async Task<bool> TryRefundPointsAsync(
            string phone, int points, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<LoyaltyAccount>()
                .Where(x => x.Phone == phone)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Points, x => x.Points + points)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
            return affected > 0;
        }

        /// <summary>
        /// Cộng điểm + tổng chi tiêu nguyên tử bằng ExecuteUpdateAsync (1 câu
        /// UPDATE ... SET Points = Points + points, TotalEarned = TotalEarned
        /// + points, TotalSpent = TotalSpent + spentAmount WHERE Phone =
        /// phone). Chạy trong DbContext/UoW hiện tại nên tham gia cùng
        /// transaction với phần còn lại của nghiệp vụ.
        /// </summary>
        public async Task<bool> TryIncrementEarnedAndSpentAsync(
            string phone, int points, decimal spentAmount,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<LoyaltyAccount>()
                .Where(x => x.Phone == phone)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Points, x => x.Points + points)
                    .SetProperty(x => x.TotalEarned, x => x.TotalEarned + points)
                    .SetProperty(x => x.TotalSpent, x => x.TotalSpent + spentAmount)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
            return affected > 0;
        }

        /// <summary>
        /// Tạo tài khoản loyalty nguyên tử (xem <see cref="ILoyaltyAccountRepository.TryInsertAsync"/>).
        /// Check-then-insert có race: 2 request cùng tạo tài khoản 1 SĐT cùng lúc
        /// đều qua được kiểm tra <c>AnyAsync</c>, request thua sẽ vi phạm unique
        /// constraint ở <c>SaveChangesAsync</c> — catch bên dưới phân biệt: nếu
        /// tài khoản đã tồn tại thật (request kia thắng) thì trả về null như
        /// chưa bao giờ tạo, chỉ ném lại khi nguyên nhân là lỗi khác.
        /// </summary>
        public async Task<LoyaltyAccount> TryInsertAsync(
            LoyaltyAccount account, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            if (account.Id == Guid.Empty)
            {
                account.Id = Guid.NewGuid();
            }

            var exists = await dbContext.Set<LoyaltyAccount>()
                .AnyAsync(x => x.Phone == account.Phone, cancellationToken);
            if (exists)
            {
                return null;
            }

            try
            {
                await dbContext.Set<LoyaltyAccount>().AddAsync(account, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                return account;
            }
            catch (DbUpdateException)
            {
                // Race ở trên: request khác đã insert cùng SĐT trước — detach
                // entity thất bại khỏi tracker (không dùng nữa) rồi xác nhận
                // lại; chỉ ném tiếp khi lỗi không phải do trùng tài khoản.
                dbContext.Entry(account).State = EntityState.Detached;
                var winnerExists = await dbContext.Set<LoyaltyAccount>()
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
