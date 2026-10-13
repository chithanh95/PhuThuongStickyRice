using System.IO;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Excel
{
    public interface IExcelWriter<T>
    {
        void Write(T data, Stream stream);
    }
}
