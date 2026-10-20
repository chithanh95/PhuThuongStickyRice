using Microsoft.AspNetCore.Http;

namespace PhuThuongStickyRice.WebAPI.Models.Products
{
    /// <summary>
    /// Model nhận file duy nhất gửi lên từ form (multipart/form-data) —
    /// dùng cho các endpoint upload/import file (ví dụ: import CSV sản phẩm).
    /// </summary>
    public class UploadFileModel
    {
        /// <summary>File người dùng gửi lên qua form.</summary>
        public IFormFile FormFile { get; set; }
    }
}
