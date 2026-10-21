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
    /// <summary>
    /// Repository truy vấn đơn hàng: mọi update trạng thái/đánh dấu lượt quay
    /// đều dùng ExecuteUpdateAsync nguyên tử (UPDATE ... WHERE có điều kiện
    /// trạng thái cũ) để chống race khi 2 request cùng xử lý 1 đơn — chỉ
    /// request đầu tiên thắng được phép chạy side-effect (cộng/hoàn điểm,
    /// hoàn ví). Các truy vấn read-only dùng AsNoTracking để giảm tải memory/CPU.
    /// </summary>
    public class OrderRepository : EfCoreRepository<StickyRiceDbContext, Order, System.Guid>, IOrderRepository
    {
        public OrderRepository(IDbContextProvider<StickyRiceDbContext> dbContextProvider,
            Volo.Abp.DependencyInjection.IAbpLazyServiceProvider lazyServiceProvider)
            : base(dbContextProvider)
        {
            // See ProductRepository: ABP property injection needs Autofac; set explicitly.
            LazyServiceProvider = lazyServiceProvider;
        }

        /// <summary>
        /// Nạp 1 đơn theo mã đơn, KÈM Lines. Cả 2 nơi dùng (tra cứu đơn cho
        /// khách, khách sửa số lượng món) đều cần Lines; từ khi bỏ AutoInclude
        /// (perf 0f1d6b8) thiếu Include ở đây làm trang tra cứu không hiện món
        /// và UpdateLineQuantities luôn ném "Đơn hàng phải có ít nhất một món."
        /// </summary>
        public async Task<Order> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();
            return await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Include(x => x.Lines).Where(x => x.OrderNumber == orderNumber), cancellationToken);
        }

        /// <summary>
        /// Kiểm tra mã đơn đã tồn tại chưa (AnyAsync — không nạp entity, nhẹ hơn
        /// GetByOrderNumberAsync); dùng khi sinh mã đơn mới để tránh trùng.
        /// </summary>
        public async Task<bool> ExistsWithOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();
            return await AsyncExecuter.AnyAsync(
                queryable.Where(x => x.OrderNumber == orderNumber), cancellationToken);
        }

        /// <summary>
        /// Nạp nhiều đơn KÈM Lines ở chế độ read-only cho export báo cáo:
        /// dedupe Id đầu vào, trả về rỗng ngay khi không có Id (tránh query
        /// WHERE IN () không cần thiết).
        /// </summary>
        public async Task<List<Order>> GetListWithLinesAsync(IEnumerable<System.Guid> orderIds, CancellationToken cancellationToken = default)
        {
            var ids = orderIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new List<Order>();
            }

            var queryable = await GetQueryableAsync();
            // Read-only cho export báo cáo: không track entity để giảm memory/CPU
            // (5000 đơn ~ 20k entity Lines đi kèm).
            return await AsyncExecuter.ToListAsync(
                queryable.AsNoTracking().Include(x => x.Lines).Where(x => ids.Contains(x.Id)),
                cancellationToken);
        }

        /// <summary>
        /// Nạp đơn theo Id ở chế độ read-only (AsNoTracking, không Include
        /// Lines): dùng cho endpoint public tra cứu đơn theo SĐT — chỉ map
        /// sang DTO rồi null AdminNote, không sửa entity, nên tracking tới
        /// 100 đơn (cột text dài Notes/AdminNote) là tốn memory/CPU vô ích.
        /// </summary>
        public async Task<List<Order>> GetOrdersByIdsNoTrackingAsync(IEnumerable<System.Guid> orderIds, CancellationToken cancellationToken = default)
        {
            var ids = orderIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new List<Order>();
            }

            var queryable = await GetQueryableAsync();
            return await AsyncExecuter.ToListAsync(
                queryable.AsNoTracking().Where(x => ids.Contains(x.Id)),
                cancellationToken);
        }

        /// <summary>
        /// Nạp các đơn ở trạng thái bếp ở chế độ read-only (AsNoTracking,
        /// không Include Lines — giống bản tracked cũ cũng không Include):
        /// màn hình bếp poll liên tục, chỉ đọc rồi map sang DTO.
        /// </summary>
        public async Task<List<Order>> GetKitchenOrdersNoTrackingAsync(
            OrderStatus[] statuses, int maxRows,
            CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();
            return await AsyncExecuter.ToListAsync(
                queryable.AsNoTracking()
                    .Where(x => statuses.Contains(x.Status))
                    .OrderBy(x => x.CreatedAt)
                    .Take(maxRows),
                cancellationToken);
        }

        /// <summary>
        /// Materialize query đơn hàng đã lọc/sắp xếp sẵn ở chế độ read-only
        /// (AsNoTracking): app service dựng filter, repository chỉ tắt
        /// tracking lúc materialize. Dùng cho các màn chỉ đọc rồi map DTO.
        /// </summary>
        public async Task<List<Order>> ToListNoTrackingAsync(
            System.Linq.IQueryable<Order> query, CancellationToken cancellationToken = default)
        {
            return await AsyncExecuter.ToListAsync(query.AsNoTracking(), cancellationToken);
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
            if (affected == 0)
            {
                return false;
            }

            await SyncTrackedRowVersionAsync(dbContext, orderId, cancellationToken);
            return true;
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
        /// Tham số <paramref name="expectedPaymentStatus"/> cho phép gắn thêm
        /// điều kiện PaymentStatus vào WHERE nguyên tử — worker tự hủy đơn
        /// chuyển khoản dùng để không hủy (và không ghi đè) đơn mà khách vừa
        /// khai báo "đã chuyển khoản" (PendingConfirmation).
        /// </summary>
        public async Task<bool> TryTransitionStatusAsync(
            System.Guid orderId, OrderStatus expectedFrom, OrderStatus to,
            PaymentStatus? expectedPaymentStatus = null,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.Set<Order>()
                .Where(x => x.Id == orderId && x.Status == expectedFrom);
            if (expectedPaymentStatus.HasValue)
            {
                var paymentStatus = expectedPaymentStatus.Value;
                query = query.Where(x => x.PaymentStatus == paymentStatus);
            }
            var affected = await query
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, to),
                    cancellationToken);
            if (affected == 0)
            {
                return false;
            }

            await SyncTrackedRowVersionAsync(dbContext, orderId, cancellationToken);
            return true;
        }

        /// <summary>
        /// Đồng bộ RowVersion mới vào entity Order đang được track (nếu có)
        /// sau khi một ExecuteUpdateAsync ghi thẳng xuống DB. SQL Server tự
        /// tăng cột timestamp ngay khi UPDATE nên entity trong RAM lập tức
        /// thành "cũ": SaveChanges sau đó (vd UpdateAsync của app service)
        /// phát UPDATE ... WHERE RowVersion = giá trị cũ, khớp 0 dòng và nổ
        /// DbUpdateConcurrencyException — user gặp 2026-10-21 khi bấm Xác
        /// nhận đơn (TryTransitionStatusAsync chạy trước UpdateAsync trong
        /// PersistStatusTransitionAsync). Chỉnh cả OriginalValue (giá trị EF
        /// dùng cho mệnh đề WHERE) lẫn giá trị hiện tại; các thay đổi khác
        /// đang chờ trên entity (CancelReason...) không bị đụng tới. Entity
        /// không được track trong context này thì không có gì để đồng bộ.
        /// </summary>
        private async Task SyncTrackedRowVersionAsync(
            StickyRiceDbContext dbContext, System.Guid orderId, CancellationToken cancellationToken)
        {
            var tracked = dbContext.Set<Order>().Local.FirstOrDefault(x => x.Id == orderId);
            if (tracked == null)
            {
                return;
            }

            var freshRowVersion = await dbContext.Set<Order>()
                .AsNoTracking()
                .Where(x => x.Id == orderId)
                .Select(x => x.RowVersion)
                .FirstAsync(cancellationToken);
            dbContext.Entry(tracked).Property(x => x.RowVersion).OriginalValue = freshRowVersion;
            tracked.RowVersion = freshRowVersion;
        }
    }
}
