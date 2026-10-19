using PhuThuongStickyRice.CrossCuttingConcerns.OS;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using EntityFrameworkCore.SqlServer.SimpleBulks.BulkDelete;
using EntityFrameworkCore.SqlServer.SimpleBulks.BulkInsert;
using EntityFrameworkCore.SqlServer.SimpleBulks.BulkMerge;
using EntityFrameworkCore.SqlServer.SimpleBulks.BulkUpdate;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Persistence.Repositories
{
    /// <summary>
    /// Repository EF Core dùng chung cho mọi aggregate root.
    /// Điểm khác biệt so với repository mặc định của ABP: tự sinh <see cref="Guid"/>
    /// cho Id rỗng khi thêm mới (AddAsync/BulkInsert) vì <c>Repository.AddAsync</c>
    /// custom này không dùng ABP <c>IEntity</c> generation — thiếu bước này sẽ
    /// gây trùng khóa chính (bug PK_AuditLogEntries 00000000-...).
    /// Đồng thời tự stamp <c>CreatedDateTime</c>/<c>UpdatedDateTime</c> bằng
    /// <c>IDateTimeProvider</c> (giờ VN) thay vì để mặc định.
    /// </summary>
    /// <typeparam name="T">Aggregate root.</typeparam>
    /// <typeparam name="TKey">Kiểu khóa chính.</typeparam>
    public class Repository<T, TKey> : IRepository<T, TKey>
        where T : Entity<TKey>, IAggregateRoot
    {
        private readonly StickyRiceDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;

        protected DbSet<T> DbSet => _dbContext.Set<T>();

        public IUnitOfWork UnitOfWork
        {
            get
            {
                return _dbContext;
            }
        }

        public Repository(StickyRiceDbContext dbContext, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
        }

        /// <summary>
        /// Thêm mới nếu Id còn rỗng, ngược lại cập nhật.
        /// </summary>
        /// <param name="entity">Entity cần thêm hoặc cập nhật.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        public async Task AddOrUpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity.Id.Equals(default(TKey)))
            {
                await AddAsync(entity, cancellationToken);
            }
            else
            {
                await UpdateAsync(entity, cancellationToken);
            }
        }

        /// <summary>
        /// Thêm entity mới: tự sinh <see cref="Guid"/> khi Id trống (chỉ cho TKey là Guid)
        /// và stamp <c>CreatedDateTime</c>.
        /// </summary>
        /// <param name="entity">Entity cần thêm.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            // FIX bug trùng khóa chính PK_* = 00000000-...: repository custom này
            // không có cơ chế sinh key của ABP, nên mọi entity Guid mới phải được
            // cấp Guid ở đây trước khi Add vào DbSet.
            if (typeof(TKey) == typeof(Guid) && entity.Id.Equals(default(TKey)))
                entity.Id = (TKey)(object)Guid.NewGuid();
            entity.CreatedDateTime = _dateTimeProvider.OffsetNow;
            await DbSet.AddAsync(entity, cancellationToken);
        }

        /// <summary>
        /// Đánh dấu entity đã thay đổi: stamp <c>UpdatedDateTime</c>.
        /// Không cần attach thủ công vì entity đã được track.
        /// </summary>
        /// <param name="entity">Entity đã sửa.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            entity.UpdatedDateTime = _dateTimeProvider.OffsetNow;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Xóa entity khỏi DbContext (xóa khi SaveChanges/UoW commit).
        /// </summary>
        /// <param name="entity">Entity cần xóa.</param>
        public void Delete(T entity)
        {
            DbSet.Remove(entity);
        }

        /// <summary>
        /// Trả về queryable gốc để caller tự compose Where/OrderBy rồi thực thi
        /// qua <c>FirstOrDefaultAsync</c>/<c>ToListAsync</c>/... (kèm async executer).
        /// </summary>
        /// <returns>IQueryable của aggregate.</returns>
        public IQueryable<T> GetAll()
        {
            return _dbContext.Set<T>();
        }

        /// <summary>Thực thi query lấy 1 bản ghi đầu tiên (hoặc null).</summary>
        public Task<T1> FirstOrDefaultAsync<T1>(IQueryable<T1> query)
        {
            return query.FirstOrDefaultAsync();
        }

        /// <summary>Thực thi query lấy đúng 1 bản ghi (hoặc null).</summary>
        public Task<T1> SingleOrDefaultAsync<T1>(IQueryable<T1> query)
        {
            return query.SingleOrDefaultAsync();
        }

        /// <summary>Thực thi query lấy danh sách.</summary>
        public Task<List<T1>> ToListAsync<T1>(IQueryable<T1> query)
        {
            return query.ToListAsync();
        }

        /// <summary>Đếm số bản ghi của query (LongCount để tránh tràn int với bảng lớn).</summary>
        public Task<long> CountAsync<T1>(IQueryable<T1> query, CancellationToken cancellationToken = default)
        {
            return query.LongCountAsync(cancellationToken);
        }

        /// <summary>
        /// Chèn hàng loạt bằng bulk insert (bỏ qua change tracker, nhanh hơn
        /// nhiều cho seed/import số lượng lớn). Tự sinh Guid cho Id trống.
        /// </summary>
        /// <param name="entities">Các entity cần chèn.</param>
        public void BulkInsert(IEnumerable<T> entities)
        {
            if (typeof(TKey) == typeof(Guid))
            {
                foreach (var entity in entities)
                {
                    if (entity.Id.Equals(default(TKey)))
                        entity.Id = (TKey)(object)Guid.NewGuid();
                }
            }
            _dbContext.BulkInsert(entities);
        }

        /// <summary>
        /// Chèn hàng loạt, chỉ chèn các cột được chọn bởi <paramref name="columnNamesSelector"/>.
        /// </summary>
        /// <param name="entities">Các entity cần chèn.</param>
        /// <param name="columnNamesSelector">Biểu thức chọn cột cần chèn.</param>
        public void BulkInsert(IEnumerable<T> entities, Expression<Func<T, object>> columnNamesSelector)
        {
            if (typeof(TKey) == typeof(Guid))
            {
                foreach (var entity in entities)
                {
                    if (entity.Id.Equals(default(TKey)))
                        entity.Id = (TKey)(object)Guid.NewGuid();
                }
            }
            _dbContext.BulkInsert(entities, columnNamesSelector);
        }

        /// <summary>
        /// Cập nhật hàng loạt, chỉ cập nhật các cột được chọn.
        /// </summary>
        /// <param name="entities">Các entity cần cập nhật.</param>
        /// <param name="columnNamesSelector">Biểu thức chọn cột cần cập nhật.</param>
        public void BulkUpdate(IEnumerable<T> entities, Expression<Func<T, object>> columnNamesSelector)
        {
            _dbContext.BulkUpdate(entities, columnNamesSelector);
        }

        /// <summary>Xóa hàng loạt (bulk delete).</summary>
        /// <param name="entities">Các entity cần xóa.</param>
        public void BulkDelete(IEnumerable<T> entities)
        {
            _dbContext.BulkDelete(entities);
        }

        /// <summary>
        /// Upsert hàng loạt: insert bản ghi mới, update các cột đã chọn cho bản ghi đã tồn tại.
        /// </summary>
        /// <param name="entities">Các entity cần merge.</param>
        /// <param name="idSelector">Biểu thức chọn khóa để đối chiếu tồn tại.</param>
        /// <param name="updateColumnNamesSelector">Cột cập nhật khi đã tồn tại.</param>
        /// <param name="insertColumnNamesSelector">Cột chèn khi chưa tồn tại.</param>
        public void BulkMerge(IEnumerable<T> entities, Expression<Func<T, object>> idSelector, Expression<Func<T, object>> updateColumnNamesSelector, Expression<Func<T, object>> insertColumnNamesSelector)
        {
            _dbContext.BulkMerge(entities, idSelector, updateColumnNamesSelector, insertColumnNamesSelector);
        }

        /// <summary>
        /// Gán giá trị row-version gốc để EF phát hiện xung đột optimistic
        /// concurrency khi SaveChanges (client gửi version đọc được trước đó).
        /// </summary>
        /// <param name="entity">Entity đang track.</param>
        /// <param name="version">Giá trị RowVersion gốc.</param>
        public void SetRowVersion(T entity, byte[] version)
        {
            _dbContext.Entry(entity).OriginalValues[nameof(entity.RowVersion)] = version;
        }

        /// <summary>
        /// Kiểm tra exception có phải lỗi xung đột concurrency của EF không.
        /// </summary>
        /// <param name="ex">Exception cần kiểm tra.</param>
        /// <returns>true nếu là <see cref="DbUpdateConcurrencyException"/>.</returns>
        public bool IsDbUpdateConcurrencyException(Exception ex)
        {
            return ex is DbUpdateConcurrencyException;
        }
    }
}
