using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    public class OrderRepository : EfCoreRepository<StickyRiceDbContext, Order, System.Guid>, IOrderRepository
    {
        public OrderRepository(IDbContextProvider<StickyRiceDbContext> dbContextProvider,
            Volo.Abp.DependencyInjection.IAbpLazyServiceProvider lazyServiceProvider)
            : base(dbContextProvider)
        {
            // See ProductRepository: ABP property injection needs Autofac; set explicitly.
            LazyServiceProvider = lazyServiceProvider;
        }

        public async Task<Order> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();
            return await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.OrderNumber == orderNumber), cancellationToken);
        }

        public async Task<bool> ExistsWithOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();
            return await AsyncExecuter.AnyAsync(
                queryable.Where(x => x.OrderNumber == orderNumber), cancellationToken);
        }

        public async Task<List<Order>> GetListWithLinesAsync(IEnumerable<System.Guid> orderIds, CancellationToken cancellationToken = default)
        {
            var ids = orderIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new List<Order>();
            }

            var queryable = await GetQueryableAsync();
            return await AsyncExecuter.ToListAsync(
                queryable.Include(x => x.Lines).Where(x => ids.Contains(x.Id)),
                cancellationToken);
        }

        /// <summary>
        /// Đánh dấu lượt quay may mắn nguyên tử bằng ExecuteUpdateAsync
        /// (1 câu UPDATE ... WHERE Id = orderId AND LuckySpinUsed = false).
        /// Chạy trong DbContext/UoW hiện tại nên rollback cùng nghiệp vụ khi
        /// các bước sau (sinh voucher) thất bại.
        /// </summary>
        public async Task<bool> TryMarkSpinUsedAsync(System.Guid orderId, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<Order>()
                .Where(x => x.Id == orderId && !x.LuckySpinUsed)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.LuckySpinUsed, true),
                    cancellationToken);
            return affected > 0;
        }

        /// <summary>
        /// Chuyển trạng thái đơn nguyên tử bằng ExecuteUpdateAsync (1 câu
        /// UPDATE ... SET Status = to WHERE Id = orderId AND Status =
        /// expectedFrom). Chạy trong DbContext/UoW hiện tại nên rollback cùng
        /// nghiệp vụ khi các bước sau thất bại. Cùng pattern với
        /// <see cref="TryMarkSpinUsedAsync"/> — chỉ request đầu tiên thắng
        /// được UPDATE mới chạy side-effect (cộng/hoàn điểm, hoàn ví, thưởng
        /// giới thiệu), request thua nhận false và phải báo lỗi rõ ràng cho
        /// người dùng thay vì xử lý tiếp.
        /// </summary>
        public async Task<bool> TryTransitionStatusAsync(
            System.Guid orderId, OrderStatus expectedFrom, OrderStatus to,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var affected = await dbContext.Set<Order>()
                .Where(x => x.Id == orderId && x.Status == expectedFrom)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, to),
                    cancellationToken);
            return affected > 0;
        }
    }
}
