using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Common
{
        /// <summary>
        /// Thông tin một link rút gọn (dùng chia sẻ khuyến mãi qua Zalo/Facebook).
        /// </summary>
        public class ShortLinkDto
        {
            public Guid Id { get; set; }
            public string Code { get; set; }
            public string TargetUrl { get; set; }
            public DateTimeOffset CreatedDateTime { get; set; }
        }

        /// <summary>
        /// Dữ liệu tạo link rút gọn mới: chỉ cần URL đích (mã code được sinh tự động).
        /// </summary>
        public class CreateShortLinkDto
        {
            [Required]
            [StringLength(2000)]
            [Url]
            public string TargetUrl { get; set; }
        }
}
