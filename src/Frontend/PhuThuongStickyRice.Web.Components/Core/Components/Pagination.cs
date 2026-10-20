using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Components
{
    /// <summary>
    /// Component phân trang dùng chung (kiểu Lexa): hiển thị tối đa 5 số trang
    /// quanh trang hiện tại (cửa sổ trượt), bắn <see cref="PageChanged"/> khi
    /// user bấm chọn trang.
    /// </summary>
    public partial class Pagination
    {
        /// <summary>Tổng số bản ghi cần phân trang.</summary>
        [Parameter]
        public long TotalItems { get; set; }

        /// <summary>Trang hiện tại (đánh số từ 1).</summary>
        [Parameter]
        public int CurrentPage { get; set; }

        /// <summary>Số bản ghi mỗi trang.</summary>
        [Parameter]
        public long PageSize { get; set; }

        /// <summary>
        /// Tổng số trang (làm tròn lên). Trả 0 khi <see cref="PageSize"/> = 0
        /// để tránh chia cho 0.
        /// </summary>
        public int TotalPages
        {
            get
            {
                return PageSize != 0 ? (int)((TotalItems + PageSize - 1) / PageSize) : 0;
            }
        }

        /// <summary>Danh sách số trang hiển thị trong cửa sổ trượt (tối đa 5 số).</summary>
        public List<int> PageNumbers { get; private set; }

        /// <summary>Callback bắn ra khi user chọn một trang hợp lệ khác.</summary>
        [Parameter]
        public EventCallback<int> PageChanged { get; set; }

        /// <summary>
        /// Xử lý click vào nút trang: bỏ qua click không hợp lệ (số trang &lt; 1,
        /// vượt quá <see cref="TotalPages"/>, hoặc đúng trang hiện tại) rồi mới
        /// invoke <see cref="PageChanged"/>.
        /// </summary>
        protected async Task HandlePageClick(int page)
        {
            // Chặn click vào nút disabled: nút "Previous" khi đang ở trang 1 vẫn fire PageChanged(0),
            // nút "Next" ở trang cuối fire PageChanged(TotalPages+1) → state phân trang bị sai và
            // màn hình đọc (screen reader) không hiểu nút đang tắt. Bỏ qua luôn click vào trang hiện tại.
            if (page < 1 || page > TotalPages || page == CurrentPage) return;
            await PageChanged.InvokeAsync(page);
        }

        /// <summary>
        /// Tính lại <see cref="PageNumbers"/> mỗi khi tham số đổi: cửa sổ trượt
        /// tối đa 5 số quanh <see cref="CurrentPage"/> (2 trước + hiện tại + 2 sau).
        /// Khi chạm biên đầu/cuối thì dịch cửa sổ sang để vẫn đủ 5 số nếu tổng
        /// số trang cho phép.
        /// </summary>
        protected override void OnParametersSet()
        {
            var startIndex = CurrentPage - 2;
            var endIndex = CurrentPage + 2;
            var totalPages = TotalPages;

            if (startIndex < 1)
            {
                endIndex = endIndex + (1 - startIndex);
                startIndex = 1;
            }

            if (endIndex > totalPages)
            {
                startIndex = startIndex - (endIndex - totalPages);
                endIndex = totalPages;
            }

            startIndex = Math.Max(startIndex, 1);
            endIndex = Math.Min(endIndex, totalPages);

            PageNumbers = new List<int>();
            for (var pageNumber = startIndex; pageNumber <= endIndex; pageNumber++)
            {
                PageNumbers.Add(pageNumber);
            }
        }
    }
}
