using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Shop
{
        public class TableQrDto
        {
            public Guid Id { get; set; }

            public string Label { get; set; }

            public string EncodedUrl { get; set; }

            public DateTime CreatedAt { get; set; }
        }

        public class CreateTableQrDto
        {
            [Required]
            [StringLength(100)]
            public string Label { get; set; }
        }

        public class TableQrGeneratedDto : TableQrDto
        {
            /// <summary>PNG bytes as base64 (data URL built client-side).</summary>
            public string PngBase64 { get; set; }
        }
}
