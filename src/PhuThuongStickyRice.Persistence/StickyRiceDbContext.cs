using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Persistence.Locks;
using System;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Persistence
{
    public class StickyRiceDbContext : DbContext, IUnitOfWork, IDataProtectionKeyContext
    {
        private IDbContextTransaction _dbContextTransaction;

        public StickyRiceDbContext(DbContextOptions<StickyRiceDbContext> options)
            : base(options)
        {
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        public async Task<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
        {
            _dbContextTransaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return _dbContextTransaction;
        }

        public async Task<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, string lockName = null, CancellationToken cancellationToken = default)
        {
            _dbContextTransaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);

            var sqlLock = new SqlDistributedLock(_dbContextTransaction.GetDbTransaction() as SqlTransaction);
            var lockScope = sqlLock.Acquire(lockName);
            if (lockScope == null)
            {
                throw new Exception($"Could not acquire lock: {lockName}");
            }

            return _dbContextTransaction;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            await _dbContextTransaction.CommitAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
