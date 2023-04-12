using System.IO;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Excel
{
    public interface IExcelReader<T>
    {
        T Read(Stream stream);
    }
}
