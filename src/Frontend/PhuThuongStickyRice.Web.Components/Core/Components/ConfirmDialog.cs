using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Components
{
    /// <summary>
    /// Hộp thoại xác nhận dùng chung: bấm nút đồng ý sẽ chạy
    /// <see cref="ConfirmEventCallback"/>, có guard chống double-submit và
    /// đóng bằng phím Escape.
    /// </summary>
    public partial class ConfirmDialog
    {
        /// <summary>Tiêu đề hiển thị trên đầu hộp thoại.</summary>
        [Parameter]
        public string Title { get; set; }

        /// <summary>Nhãn nút đồng ý.</summary>
        [Parameter]
        public string YesText { get; set; }

        /// <summary>Nhãn nút hủy/bỏ qua.</summary>
        [Parameter]
        public string NoText { get; set; }

        /// <summary>Trạng thái mở/đóng của hộp thoại.</summary>
        public bool ShowDialog { get; set; }

        // Chống double-submit: bấm nút "Có/Xóa" 2 lần liên tiếp (click đúp)
        // sẽ gọi ConfirmEventCallback 2 lần — lần 2 thường throw (vd xóa 1
        // mã giảm giá đã bị xóa) khiến user thấy lỗi dù thao tác đã thành công.
        private bool _submitting;

        /// <summary>Callback chạy khi user bấm nút đồng ý (truyền true).</summary>
        [Parameter]
        public EventCallback<bool> ConfirmEventCallback { get; set; }

        /// <summary>Nội dung thông điệp hiển thị trong thân hộp thoại.</summary>
        [Parameter]
        public RenderFragment Message { get; set; }

        /// <summary>Mở hộp thoại và yêu cầu re-render.</summary>
        public void Show()
        {
            ShowDialog = true;
            StateHasChanged();
        }

        /// <summary>Đóng hộp thoại và yêu cầu re-render.</summary>
        public void Close()
        {
            ShowDialog = false;
            StateHasChanged();
        }

        /// <summary>
        /// UIUX-1019 Agent4 (a11y): cho phép đóng dialog bằng phím Escape.
        /// Người dùng bàn phím cần cách thoát chuẩn khỏi hộp thoại mà không
        /// phải tab tới nút × hoặc nút Hủy.
        /// </summary>
        protected void HandleKeyDown(KeyboardEventArgs e)
        {
            if (e?.Key == "Escape")
            {
                Close();
            }
        }

        /// <summary>
        /// Submit nút đồng ý: guard <c>_submitting</c> chặn click đúp gọi
        /// <see cref="ConfirmEventCallback"/> 2 lần, rồi invoke callback.
        /// </summary>
        protected async Task HandleValidSubmit()
        {
            if (_submitting)
            {
                return;
            }

            _submitting = true;
            StateHasChanged();
            try
            {
                await ConfirmEventCallback.InvokeAsync(true);
            }
            finally
            {
                _submitting = false;
                StateHasChanged();
            }
        }
    }
}
