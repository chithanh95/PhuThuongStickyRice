using System;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Locks
{
    public class CouldNotAcquireLockException : Exception
    {
        public CouldNotAcquireLockException()
        {
        }

        public CouldNotAcquireLockException(string message)
            : base(message)
        {
        }

        public CouldNotAcquireLockException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
