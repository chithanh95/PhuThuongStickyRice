using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Common
{
        public class ShortLinkDto
        {
            public Guid Id { get; set; }
            public string Code { get; set; }
            public string TargetUrl { get; set; }
            public DateTimeOffset CreatedDateTime { get; set; }
        }

        public class CreateShortLinkDto
        {
            [Required]
            [StringLength(2000)]
            [Url]
            public string TargetUrl { get; set; }
        }
}
