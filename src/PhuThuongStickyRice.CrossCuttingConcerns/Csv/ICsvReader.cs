using System.Collections.Generic;
using System.IO;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Csv
{
    public interface ICsvReader<T>
    {
        IEnumerable<T> Read(Stream stream);
    }
}
