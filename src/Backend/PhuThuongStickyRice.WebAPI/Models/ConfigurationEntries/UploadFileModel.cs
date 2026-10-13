using Microsoft.AspNetCore.Http;

namespace PhuThuongStickyRice.WebAPI.Models.ConfigurationEntries
{
    public class UploadFileModel
    {
        public IFormFile FormFile { get; set; }
    }
}
