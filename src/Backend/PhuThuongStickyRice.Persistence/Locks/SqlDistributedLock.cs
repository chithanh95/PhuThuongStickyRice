using PhuThuongStickyRice.CrossCuttingConcerns.Locks;
using EntityFrameworkCore.SqlServer.SimpleBulks.Extensions;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace PhuThuongStickyRice.Persistence.Locks
{
    /// <summary>
    /// Distributed lock trên SQL Server bằng <c>sp_getapplock</c>. Lock owner
    /// là "Transaction" khi tạo từ <see cref="SqlTransaction"/> (tự release khi
    /// transaction commit/rollback — an toàn cho lock trong nghiệp vụ), ngược
    /// lại là "Session" (phải release thủ công qua scope).
    /// </summary>
    public class SqlDistributedLock : IDistributedLock
    {
        public const int AlreadyHeldReturnCode = 103;

        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;
        private readonly string _connectionString;

        public bool HasTransaction
        {
            get
            {
                return _transaction != null;
            }
        }

        public SqlDistributedLock(SqlConnection connection)
        {
            _connection = connection;
        }

        public SqlDistributedLock(SqlTransaction transaction)
        {
            _transaction = transaction;
            _connection = _transaction.Connection;
        }

        public SqlDistributedLock(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new SqlConnection(connectionString);
        }

        /// <summary>
        /// Lấy lock, chờ không giới hạn cho tới khi có lock (lockTimeout -1).
        /// Trả về scope để release, hoặc null nếu lock đang bị giữ (mã 103).
        /// </summary>
        public IDistributedLockScope Acquire(string lockName)
        {
            SqlParameter returnValue;
            // SqlCommand giữ handle phía server — dispose ngay sau khi dùng xong.
            // commandTimeout 0 (không chờ kết nối), lockTimeout -1 (chờ lock vô hạn).
            using var acquireCommand = CreateAcquireCommand(0, lockName, -1, out returnValue);

            acquireCommand.ExecuteNonQuery();

            if (ParseReturnCode((int)returnValue.Value))
            {
                return new SqlDistributedLockScope(_connection, _transaction, lockName);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Thử lấy lock, không chờ (lockTimeout 0): lấy được trả về scope,
        /// không lấy được trả về null ngay — dùng khi caller có phương án dự
        /// phòng thay vì block. Command timeout 30s cho sp_getapplock.
        /// </summary>
        public IDistributedLockScope TryAcquire(string lockName)
        {
            SqlParameter returnValue;
            // SqlCommand giữ handle phía server — dispose ngay sau khi dùng xong.
            // Tham số theo thứ tự (commandTimeout, lockName, lockTimeout):
            // 30s chờ command, 0 = thử lấy lock 1 lần, không chờ.
            using var acquireCommand = CreateAcquireCommand(30, lockName, 0, out returnValue);

            acquireCommand.ExecuteNonQuery();

            if (ParseReturnCode((int)returnValue.Value))
            {
                return new SqlDistributedLockScope(_connection, _transaction, lockName);
            }
            else
            {
                return null;
            }
        }

        private SqlCommand CreateAcquireCommand(int commandTimeout, string lockName, int lockTimeout, out SqlParameter returnValue)
        {
            _connection.EnsureOpen();

            SqlCommand command = _connection.CreateCommand();
            command.Transaction = _transaction;

            returnValue = command.Parameters.Add(new SqlParameter { ParameterName = "Result", DbType = DbType.Int32, Direction = ParameterDirection.Output });
            command.CommandText =
                $@"IF APPLOCK_MODE('public', @Resource, @LockOwner) != 'NoLock' {(HasTransaction ? " OR APPLOCK_MODE('public', @Resource, 'Session') != 'NoLock'" : string.Empty)}
                            SET @Result = {AlreadyHeldReturnCode}
                        ELSE
                            EXEC @Result = dbo.sp_getapplock @Resource = @Resource, @LockMode = @LockMode, @LockOwner = @LockOwner, @LockTimeout = @LockTimeout, @DbPrincipal = 'public'"
                      ;

            command.CommandTimeout = commandTimeout;

            command.Parameters.Add(new SqlParameter("Resource", lockName));
            command.Parameters.Add(new SqlParameter("LockMode", "Exclusive"));
            command.Parameters.Add(new SqlParameter("LockOwner", HasTransaction ? "Transaction" : "Session"));
            command.Parameters.Add(new SqlParameter("LockTimeout", lockTimeout));

            return command;
        }

        /// <summary>
        /// sp_getapplock exit codes documented at
        /// https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql#return-code-values
        /// </summary>
        /// <param name="returnCode">code returned after calling sp_getapplock</param>
        /// <returns>true/false</returns>
        public static bool ParseReturnCode(int returnCode)
        {
            switch (returnCode)
            {
                case 0:
                case 1:
                    return true;
                case -1:
                    return false;
                case -2:
                    throw new OperationCanceledException("The lock request was canceled.");
                case -3:
                    // CA2201: không ném Exception chung chung — dùng exception chuyên
                    // dụng của tính năng lock để caller phân biệt được với lỗi khác
                    // (nhất quán với SqlServerDistributedLockProvider).
                    throw new CouldNotAcquireLockException("The lock request was chosen as a deadlock victim.");
                case -999:
                    throw new ArgumentException("parameter validation or other error");
                case AlreadyHeldReturnCode:
                    return false;
            }

            if (returnCode <= 0)
            {
                throw new InvalidOperationException($"Could not acquire lock with return code: {returnCode}");
            }

            return false;
        }

        public void Dispose()
        {
            // CA1816: gọi SuppressFinalize để derived class có finalizer sau này
            // không phải re-implement IDisposable.
            if (!string.IsNullOrEmpty(_connectionString))
            {
                _connection.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}
