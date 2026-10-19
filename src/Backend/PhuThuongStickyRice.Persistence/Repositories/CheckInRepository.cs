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
    public class CheckInRepository
        : EfCoreRepository<StickyRiceDbContext, CheckIn, System.Guid>, ICheckInRepository
    {
        public CheckInRepository(IDbContextProvider<StickyRiceDbContext> dbContextProvider,
            Volo.Abp.DependencyInjection.IAbpLazyServiceProvider lazyServiceProvider)
            : base(dbContextProvider)
        {
            // See ProductRepository: ABP property injection needs Autofac; set explicitly.
            LazyServiceProvider = lazyServiceProvider;
        }

        /// <summary>
        /// Ghi nhận điểm danh nguyên tử (xem <see cref="ICheckInRepository.TryInsertAsync"/>).
        /// Chạy trong DbContext/UoW hiện tại nên tham gia cùng transaction với
        /// phần còn lại của nghiệp vụ điểm danh.
        /// </summary>
        public async Task<bool> TryInsertAsync(
            CheckIn checkIn, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            if (checkIn.Id == Guid.Empty)
            {
                // Không qua ABP InsertAsync nên tự cấp Id (config có
                // newsequentialid() nhưng SQLite test không hỗ trợ).
                checkIn.Id = Guid.NewGuid();
            }

            // Đường nhanh: đã điểm danh rồi -> không insert.
            var exists = await dbContext.Set<CheckIn>()
                .AnyAsync(x => x.Phone == checkIn.Phone && x.Date == checkIn.Date, cancellationToken);
            if (exists)
            {
                return false;
            }

            try
            {
                await dbContext.Set<CheckIn>().AddAsync(checkIn, cancellationToken);
                // Flush ngay (tương đương autoSave: true của ABP InsertAsync)
                // để unique (Phone, Date) phát huy tác dụng trong race.
                await dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException)
            {
                // Race: 2 request cùng qua pre-check, unique index chỉ cho 1
                // bên insert. Detach entity lỗi — nếu không, SaveChanges ở cuối
                // UoW sẽ thử insert lại và ném lỗi lần nữa, biến kết quả đúng
                // ("đã điểm danh") của bên thua thành 500.
                dbContext.Entry(checkIn).State = EntityState.Detached;
                return !await dbContext.Set<CheckIn>()
                    .AnyAsync(x => x.Phone == checkIn.Phone && x.Date == checkIn.Date, cancellationToken);
            }
        }
    }
}
