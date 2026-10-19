using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Common.DTOs
{
    /// <summary>
    /// Kết quả phân trang chung: tổng số bản ghi (để client vẽ phân trang)
    /// cùng danh sách bản ghi của trang hiện tại.
    /// </summary>
    /// <typeparam name="T">Kiểu DTO của từng bản ghi.</typeparam>
    public class Paged<T>
    {
        public long TotalItems { get; set; }

        public List<T> Items { get; set; }
    }
}
