using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Messenger
{
        public class MessageTemplateDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Channel { get; set; }
            public string Body { get; set; }
        }

        public class CreateUpdateMessageTemplateDto
        {
            [Required]
            [StringLength(100)]
            public string Name { get; set; }

            /// <summary>"SMS" or "Zalo".</summary>
            [Required]
            [StringLength(20)]
            public string Channel { get; set; }

            [Required]
            [StringLength(2000)]
            public string Body { get; set; }
        }
}
