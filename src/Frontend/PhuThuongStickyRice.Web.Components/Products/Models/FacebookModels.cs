using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Products.Models
{
    /// <summary>Item 130: whether FB auto-post is configured.</summary>
    public class FacebookStatusModel
    {
        public bool Configured { get; set; }

        public string PageId { get; set; }
    }

    /// <summary>Item 130: result of a publish attempt.</summary>
    public class FacebookPublishResultModel
    {
        public string Status { get; set; }

        public string PostUrl { get; set; }

        public string Message { get; set; }
    }

    /// <summary>Item 130: one row of the FB auto-post history.</summary>
    public class FacebookPostLogModel
    {
        public Guid Id { get; set; }

        public Guid? ProductId { get; set; }

        public string ProductName { get; set; }

        public string PostUrl { get; set; }

        public string Status { get; set; }

        public string ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
