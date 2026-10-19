namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Marker interface cho một query (truy vấn đọc dữ liệu) trong mẫu CQRS.
    /// Query không được làm thay đổi dữ liệu — chỉ đọc và trả về kết quả.
    /// </summary>
    /// <typeparam name="TResult">Kiểu dữ liệu query trả về.</typeparam>
    public interface IQuery<TResult>
    {
    }
}
