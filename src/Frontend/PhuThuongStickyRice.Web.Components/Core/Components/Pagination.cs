using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Components
{
    public partial class Pagination
    {
        [Parameter]
        public long TotalItems { get; set; }

        [Parameter]
        public int CurrentPage { get; set; }

        [Parameter]
        public long PageSize { get; set; }

        public int TotalPages
        {
            get
            {
                return PageSize != 0 ? (int)((TotalItems + PageSize - 1) / PageSize) : 0;
            }
        }

        public List<int> PageNumbers { get; private set; }

        [Parameter]
        public EventCallback<int> PageChanged { get; set; }

        protected async Task HandleClick(int page)
        {
            // Chặn click vào nút disabled: nút "Previous" khi đang ở trang 1 vẫn fire PageChanged(0),
            // nút "Next" ở trang cuối fire PageChanged(TotalPages+1) → state phân trang bị sai và
            // màn hình đọc (screen reader) không hiểu nút đang tắt. Bỏ qua luôn click vào trang hiện tại.
            if (page < 1 || page > TotalPages || page == CurrentPage) return;
            await PageChanged.InvokeAsync(page);
        }

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
