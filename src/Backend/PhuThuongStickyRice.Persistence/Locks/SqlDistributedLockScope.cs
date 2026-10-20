using PhuThuongStickyRice.CrossCuttingConcerns.Locks;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace PhuThuongStickyRice.Persistence.Locks
{
    /// <summary>
    /// Handle của một distributed lock SQL Server đang giữ: tự release lock
    /// khi dispose (chỉ với lock cấp Session; lock cấp Transaction tự release
    /// khi transaction commit/rollback nên không cần làm gì thêm).
    /// </summary>
    public class SqlDistributedLockScope : IDistributedLockScope
    {
        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;
        private readonly string _lockName;

        /// <summary>Cho biết lock đang gắn với transaction hay session độc lập.</summary>
        public bool HasTransaction
        {
            get
            {
                return _transaction != null;
            }
        }

        /// <summary>
        /// Tạo scope quản lý lock SQL Server đang giữ.
        /// </summary>
        /// <param name="connection">Kết nối đã acquire lock.</param>
        /// <param name="transaction">Transaction chứa lock (null nếu lock cấp Session).</param>
        /// <param name="lockName">Tên lock đã giữ (resource của sp_getapplock).</param>
        public SqlDistributedLockScope(SqlConnection connection, SqlTransaction transaction, string lockName)
        {
            _connection = connection;
            _transaction = transaction;
            _lockName = lockName;
        }

        /// <summary>
        /// Release lock cấp Session qua <c>sp_releaseapplock</c>; ném
        /// <see cref="InvalidOperationException"/> nếu release thất bại để lỗi
        /// lock không bị nuốt im lặng. Lock cấp Transaction bỏ qua vì server
        /// tự release khi transaction kết thúc.
        /// </summary>
        public void Dispose()
        {
            if (!HasTransaction)
            {
                SqlParameter returnValue;
                // SqlCommand giữ handle phía server — dispose ngay sau khi dùng xong.
                using var releaseCommand = CreateReleaseCommand(_lockName, false, out returnValue);
                releaseCommand.ExecuteNonQuery();

                // ParseReturnCode ném InvalidOperationException nếu release thất bại;
                // khối if trống trước đây không làm gì với giá trị true trả về nên
                // bỏ để code gọn — behavior giữ nguyên (vẫn ném khi lỗi).
                ParseReturnCode((int)returnValue.Value);
            }

            // CA1816: gọi SuppressFinalize để derived class có finalizer sau này
            // không phải re-implement IDisposable.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Kiểm tra lock hiện tại có còn đang giữ không bằng cách hỏi server
        /// (<c>APPLOCK_MODE</c>). Dùng để phát hiện lock bị release sớm
        /// (vd connection drop) trước khi thực hiện công việc quan trọng.
        /// </summary>
        /// <returns><c>true</c> nếu lock vẫn đang giữ, ngược lại <c>false</c>.</returns>
        public bool StillHoldingLock()
        {
            // SqlCommand giữ handle phía server — dispose ngay sau khi dùng xong.
            using var command = _connection.CreateCommand();
            command.Transaction = _transaction;

            command.CommandText = @"SELECT APPLOCK_MODE('public', @Resource, @LockOwner)";
            command.Parameters.Add(new SqlParameter("Resource", _lockName));
            command.Parameters.Add(new SqlParameter("LockOwner", HasTransaction ? "Transaction" : "Session"));
            var lockMode = (string)command.ExecuteScalar();

            return lockMode != "NoLock";
        }

        private SqlCommand CreateReleaseCommand(string lockName, bool isTry, out SqlParameter returnValue)
        {
            var command = _connection.CreateCommand();
            command.Transaction = _transaction;

            if (isTry)
            {
                command.CommandText =
                    @"IF APPLOCK_MODE('public', @Resource, @LockOwner) != 'NoLock'
                        EXEC @Result = dbo.sp_releaseapplock @Resource, @LockOwner
                      ELSE
                        SET @Result = 0"
                ;
            }
            else
            {
                command.CommandText = "dbo.sp_releaseapplock";
                command.CommandType = CommandType.StoredProcedure;
            }

            command.Parameters.Add(new SqlParameter("Resource", lockName));
            command.Parameters.Add(new SqlParameter("LockOwner", HasTransaction ? "Transaction" : "Session"));

            if (isTry)
            {
                returnValue = command.Parameters.Add(new SqlParameter { ParameterName = "Result", DbType = DbType.Int32, Direction = ParameterDirection.Output });
            }
            else
            {
                returnValue = command.Parameters.Add(new SqlParameter { DbType = DbType.Int32, Direction = ParameterDirection.ReturnValue });
            }

            return command;
        }

        /// <summary>
        /// sp_releaseapplock exit codes documented at
        /// https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-releaseapplock-transact-sql#return-code-values
        /// </summary>
        /// <param name="returnCode">code returned after calling sp_releaseapplock</param>
        /// <returns>true/false</returns>
        public static bool ParseReturnCode(int returnCode)
        {
            if (returnCode >= 0)
            {
                return true;
            }

            throw new InvalidOperationException($"Could not release lock with return code: {returnCode}");
        }
    }
}
