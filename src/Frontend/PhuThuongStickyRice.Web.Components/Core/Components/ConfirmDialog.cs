using Microsoft.AspNetCore.Components;
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
