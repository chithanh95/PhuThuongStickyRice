using System;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Locks
{
    public interface IDistributedLockScope : IDisposable
    {
        bool StillHoldingLock();
    }
}
