namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Marker interface cho một command (hành động thay đổi dữ liệu) trong
    /// mẫu CQRS: tách riêng việc "ghi" (command) khỏi việc "đọc" (query)
    /// để mỗi handler chỉ lo một trách nhiệm.
    /// </summary>
    public interface ICommand
    {
    }
}
