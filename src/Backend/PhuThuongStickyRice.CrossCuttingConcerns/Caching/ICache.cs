using System;

namespace PhuThuongStickyRice.CrossCuttingConcerns.Caching
{
    /// <summary>
    /// Cache trong bộ nhớ (in-memory) dùng chung: lưu tạm kết quả tính toán
    /// đắt đỏ (vd menu sản phẩm) để giảm tải DB. Chỉ dùng trong 1 instance
    /// app — không chia sẻ giữa các server.
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// Lưu <paramref name="item"/> vào cache với khóa <paramref name="key"/>,
        /// tự hết hạn sau <paramref name="timeSpan"/>.
        /// </summary>
        /// <param name="key">Khóa cache duy nhất.</param>
        /// <param name="item">Dữ liệu cần lưu.</param>
        /// <param name="timeSpan">Thời gian sống của mục cache.</param>
        void Add(string key, object item, TimeSpan timeSpan);

        /// <summary>
        /// Xóa mục cache theo khóa; khóa không tồn tại thì bỏ qua.
        /// </summary>
        /// <param name="key">Khóa cache cần xóa.</param>
        void Remove(string key);

        /// <summary>
        /// Lấy mục cache theo khóa; trả về null khi chưa có hoặc đã hết hạn.
        /// </summary>
        /// <param name="key">Khóa cache cần đọc.</param>
        /// <returns>Object đã lưu, hoặc null.</returns>
        object Get(string key);
    }
}
