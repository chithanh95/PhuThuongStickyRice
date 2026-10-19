using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Components
{
    public partial class ConfirmDialog
    {
        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public string YesText { get; set; }

        [Parameter]
        public string NoText { get; set; }

        public bool ShowDialog { get; set; }

        // Chống double-submit: bấm nút "Có/Xóa" 2 lần liên tiếp (click đúp)
        // sẽ gọi ConfirmEventCallback 2 lần — lần 2 thường throw (vd xóa 1
        // mã giảm giá đã bị xóa) khiến user thấy lỗi dù thao tác đã thành công.
        private bool _submitting;

        [Parameter]
        public EventCallback<bool> ConfirmEventCallback { get; set; }

        [Parameter]
        public RenderFragment Message { get; set; }

        public void Show()
        {
            ShowDialog = true;
            StateHasChanged();
        }

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
