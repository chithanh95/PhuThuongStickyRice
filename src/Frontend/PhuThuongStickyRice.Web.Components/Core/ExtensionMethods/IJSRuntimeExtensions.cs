using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components
{
    /// <summary>
    /// Extension debug cho <see cref="IJSRuntime"/>: ghi log/table/alert ra console trình duyệt.
    /// </summary>
    public static class IJSRuntimeExtensions
    {
        /// <summary>Ghi giá trị ra console.log kèm timestamp.</summary>
        public static async Task Log(this IJSRuntime jsRuntime, object value)
        {
            await jsRuntime.InvokeVoidAsync("console.log", $"[{DateTimeOffset.Now}]", value);
        }

        /// <summary>Ghi giá trị ra console.table kèm timestamp.</summary>
        public static async Task LogTable(this IJSRuntime jsRuntime, object value)
        {
            await jsRuntime.InvokeVoidAsync("console.log", $"[{DateTimeOffset.Now}]");
            await jsRuntime.InvokeVoidAsync("console.table", value);
        }

        /// <summary>Hiện hộp thoại alert của trình duyệt.</summary>
        public static async Task Alert(this IJSRuntime jsRuntime, string text)
        {
          await jsRuntime.InvokeVoidAsync("alert", text);
        }
    }
}
