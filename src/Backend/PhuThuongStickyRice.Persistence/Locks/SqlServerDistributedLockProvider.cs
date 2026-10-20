using PhuThuongStickyRice.CrossCuttingConcerns.Locks;
using PhuThuongStickyRice.CrossCuttingConcerns.OS;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;

namespace PhuThuongStickyRice.Persistence.Locks
{
    /// <summary>
    /// Khóa phân tán dùng bảng [Locks] trên SQL Server (MERGE/UPSERT theo từng thực thể).
    /// </summary>
    public class SqlServerDistributedLockProvider : IDistributedLockProvider
    {
        private readonly StickyRiceDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;

        public SqlServerDistributedLockProvider(StickyRiceDbContext dbContext, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
        }

        /// <summary>
        /// Tạo hàng lock nếu chưa có (MERGE ... WITH (HOLDLOCK) để upsert
        /// nguyên tử — tránh hai tiến trình cùng tạo trùng khi chưa có dòng
        /// nào, vì bản ghi lock chưa tồn tại thì chưa có OwnerId để tranh giành).
        /// </summary>
        private void CreateLock(string entityName, string entityId)
        {
            string sql = @"
            merge into 
                [dbo].[Locks] with (holdlock) t  
            using 
                (values (@entityName, @entityId)) s([EntityName], [EntityId]) 
            on 
                t.[EntityName] = s.[EntityName] and t.[EntityId] = s.[EntityId] 
            when not matched then 
                insert ([EntityName], [EntityId]) values (s.[EntityName], s.[EntityId]);
            ";

            _dbContext.Database.ExecuteSqlRaw(sql,
                  new SqlParameter("entityName", entityName),
                  new SqlParameter("entityId", entityId));
        }

        /// <summary>
        /// Chiếm lock cho một thực thể. Thứ tự: (1) gia hạn trước để giữ lock
        /// của chính mình nếu đã giữ (chống mất lock giữa chừng khi tác vụ
        /// chạy lâu hơn <paramref name="expiration"/>); (2) nếu không, UPDATE
        /// chỉ thành công khi lock còn trống (OwnerId NULL) hoặc đã hết hạn —
        /// điều kiện trong WHERE là chốt atomicity, không dùng SELECT-then-UPDATE.
        /// </summary>
        /// <param name="entityName">Tên loại thực thể (nhóm lock).</param>
        /// <param name="entityId">Id thực thể cần khóa.</param>
        /// <param name="ownerId">Id chủ sở hữu lock (thường là Guid của tác vụ/instance).</param>
        /// <param name="expiration">Thời gian giữ lock tối đa — quá hạn thì lock khác được chiếm, chống chết kẹt khi tiến trình crash.</param>
        /// <returns>True khi chiếm (hoặc đã giữ) lock thành công.</returns>
        public bool AcquireLock(string entityName, string entityId, string ownerId, TimeSpan expiration)
        {
            CreateLock(entityName, entityId);

            if (ExtendLock(entityName, entityId, ownerId, expiration))
            {
                return true;
            }

            var now = _dateTimeProvider.OffsetNow;
            var expired = now + expiration;

            string sql = @"
            Update Locks set OwnerId = @OwnerId, 
            AcquiredDateTime = @AcquiredDateTime,
            ExpiredDateTime = @ExpiredDateTime
            where EntityId = @EntityId 
            and EntityName = @EntityName 
            and (OwnerId is NULL or ExpiredDateTime < @AcquiredDateTime)";

            // Số dòng bị ảnh hưởng bởi câu UPDATE — > 0 nghĩa là chiếm lock thành công.
            var rowsAffected = _dbContext.Database.ExecuteSqlRaw(sql,
                  new SqlParameter("EntityName", entityName),
                  new SqlParameter("EntityId", entityId),
                  new SqlParameter("OwnerId", ownerId),
                  new SqlParameter("AcquiredDateTime", SqlDbType.DateTimeOffset) { Value = now },
                  new SqlParameter("ExpiredDateTime", SqlDbType.DateTimeOffset) { Value = expired });

            return rowsAffected > 0;
        }

        /// <summary>
        /// Gia hạn lock của chính mình (dời <c>ExpiredDateTime</c>). Chỉ chủ
        /// sở hữu hiện tại mới gia hạn được — OwnerId trong WHERE chống
        /// tiến trình khác vô tình "cướp" gia hạn lock không phải của mình.
        /// </summary>
        /// <param name="entityName">Tên loại thực thể.</param>
        /// <param name="entityId">Id thực thể.</param>
        /// <param name="ownerId">Phải khớp chủ sở hữu hiện tại của lock.</param>
        /// <param name="expiration">Thời gian gia hạn tính từ hiện tại.</param>
        /// <returns>True khi vẫn đang giữ lock và gia hạn thành công.</returns>
        public bool ExtendLock(string entityName, string entityId, string ownerId, TimeSpan expiration)
        {
            var now = _dateTimeProvider.OffsetNow;
            var expired = now + expiration;

            string sql = @"
            Update Locks set ExpiredDateTime = @ExpiredDateTime
            where EntityId = @EntityId 
            and EntityName = @EntityName 
            and OwnerId = @OwnerId";

            // Số dòng bị ảnh hưởng bởi câu UPDATE — > 0 nghĩa là gia hạn lock thành công.
            var rowsAffected = _dbContext.Database.ExecuteSqlRaw(sql,
                  new SqlParameter("EntityName", entityName),
                  new SqlParameter("EntityId", entityId),
                  new SqlParameter("OwnerId", ownerId),
                  new SqlParameter("ExpiredDateTime", SqlDbType.DateTimeOffset) { Value = expired });

            return rowsAffected > 0;
        }

        /// <summary>
        /// Nhả lock của chính mình (không xóa hàng, chỉ đặt OwnerId NULL).
        /// Giữ lại hàng lock để lần chiếm tiếp không phải MERGE tạo lại —
        /// tiết kiệm một vòng DB write trên đường nóng.
        /// </summary>
        /// <param name="entityName">Tên loại thực thể.</param>
        /// <param name="entityId">Id thực thể.</param>
        /// <param name="ownerId">Chỉ chủ sở hữu hiện tại mới nhả được.</param>
        /// <returns>Luôn true (nhả idempotent — không giữ lock thì không làm gì).</returns>
        public bool ReleaseLock(string entityName, string entityId, string ownerId)
        {
            string sql = @"
            Update Locks set OwnerId = NULL, 
            AcquiredDateTime = NULL,
            ExpiredDateTime = NULL
            where EntityId = @EntityId 
            and EntityName = @EntityName 
            and OwnerId = @OwnerId";

            _ = _dbContext.Database.ExecuteSqlRaw(sql,
                  new SqlParameter("EntityName", entityName),
                  new SqlParameter("EntityId", entityId),
                  new SqlParameter("OwnerId", ownerId));

            return true;
        }

        /// <summary>
        /// Nhả toàn bộ lock đang giữ bởi một owner — dùng khi tắt instance
        /// hoặc dọn dẹp sau khi tác vụ dài kết thúc, chống rò rỉ lock.
        /// </summary>
        /// <param name="ownerId">Id chủ sở hữu cần nhả hết lock.</param>
        /// <returns>Luôn true.</returns>
        public bool ReleaseLocks(string ownerId)
        {
            string sql = @"
            Update Locks set OwnerId = NULL, 
            AcquiredDateTime = NULL,
            ExpiredDateTime = NULL
            where OwnerId = @OwnerId";

            _ = _dbContext.Database.ExecuteSqlRaw(sql,
                  new SqlParameter("OwnerId", ownerId));

            return true;
        }

        /// <summary>
        /// Nhả mọi lock đã quá <c>ExpiredDateTime</c> — chạy định kỳ (job dọn
        /// dẹp) để thu hồi lock bị kẹt khi tiến trình giữ lock crash mà không
        /// kịp nhả.
        /// </summary>
        /// <returns>Luôn true.</returns>
        public bool ReleaseExpiredLocks()
        {
            var now = _dateTimeProvider.OffsetNow;

            string sql = @"
            Update Locks set OwnerId = NULL, 
            AcquiredDateTime = NULL,
            ExpiredDateTime = NULL
            where ExpiredDateTime < @now";

            _ = _dbContext.Database.ExecuteSqlRaw(sql,
                  new SqlParameter("now", SqlDbType.DateTimeOffset) { Value = now });

            return true;
        }

        /// <summary>
        /// Chiếm lock, ném <see cref="CouldNotAcquireLockException"/> nếu không
        /// chiếm được — dùng trong các flow bắt buộc loại trừ lẫn nhau (ví dụ
        /// sinh mã đơn tuần tự) thay vì tự kiểm tra bool rồi throw tay.
        /// </summary>
        /// <param name="entityName">Tên loại thực thể.</param>
        /// <param name="entityId">Id thực thể.</param>
        /// <param name="ownerId">Id chủ sở hữu lock.</param>
        /// <param name="expiration">Thời gian giữ lock tối đa.</param>
        /// <exception cref="CouldNotAcquireLockException">Ném khi lock đang bị giữ bởi owner khác và chưa hết hạn.</exception>
        public void EnsureAcquiringLock(string entityName, string entityId, string ownerId, TimeSpan expiration)
        {
            if (!AcquireLock(entityName, entityId, ownerId, expiration))
            {
                throw new CouldNotAcquireLockException();
            }
        }
    }
}
