using System;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Locks
{
    public interface ILockManager
    {
        bool AcquireLock(string entityName, string entityId, string ownerId, TimeSpan expiration);

        bool ExtendLock(string entityName, string entityId, string ownerId, TimeSpan expiration);

        bool ReleaseLock(string entityName, string entityId, string ownerId);

        bool ReleaseLocks(string ownerId);

        bool ReleaseExpiredLocks();

        void EnsureAcquiringLock(string entityName, string entityId, string ownerId, TimeSpan expiration);
    }
}
