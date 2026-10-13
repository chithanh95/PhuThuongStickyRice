using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Application.FileEntries.DTOs
{
    public class FileDeletedEvent
    {
        public FileEntry FileEntry { get; set; }
    }
}
