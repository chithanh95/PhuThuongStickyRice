using System;
using Volo.Abp.Application.Dtos;

namespace PhuThuongStickyRice.Application.Facebook
{
    /// <summary>
    /// Item 130: one row of the Facebook auto-post history (admin view).
    /// </summary>
    public class FacebookPostLogDto : EntityDto<Guid>
    {
        public Guid? ProductId { get; set; }

        public string ProductName { get; set; }

        public string PostUrl { get; set; }

        /// <summary>Posted / Failed / Skipped.</summary>
        public string Status { get; set; }

        public string ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Item 130: result of a publish attempt, returned to the admin UI.
    /// </summary>
    public class FacebookPublishResultDto
    {
        public string Status { get; set; }

        public string PostUrl { get; set; }

        /// <summary>Human-readable (Vietnamese) message for the admin UI.</summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Item 130: whether auto-posting is configured (used to default the
    /// "Đăng lên Facebook" checkbox on the product form).
    /// </summary>
    public class FacebookStatusDto
    {
        public bool Configured { get; set; }

        public string PageId { get; set; }
    }
}
