using Microsoft.AspNetCore.Http;

namespace PhuThuongStickyRice.WebAPI.Models.Products
{
    public class UploadFileModel
    {
        public IFormFile FormFile { get; set; }
    }
}
