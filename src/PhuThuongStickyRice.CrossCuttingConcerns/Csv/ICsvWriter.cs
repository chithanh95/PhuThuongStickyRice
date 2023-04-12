using System.Collections.Generic;
using System.IO;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Csv
{
    public interface ICsvWriter<T>
    {
        void Write(IEnumerable<T> collection, Stream stream);
    }
}
