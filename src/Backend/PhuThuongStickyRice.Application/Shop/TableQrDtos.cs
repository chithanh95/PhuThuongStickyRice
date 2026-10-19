using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Shop
{
        /// <summary>QR bàn hiển thị cho khách quét gọi món: nhãn bàn (vd "Bàn 01"), URL đã mã hóa chứa mã bàn và ngày tạo.</summary>
        public class TableQrDto
        {
            public Guid Id { get; set; }

            public string Label { get; set; }

            public string EncodedUrl { get; set; }

            public DateTime CreatedAt { get; set; }
        }

        /// <summary>Input admin tạo QR cho một bàn: nhãn bàn (bắt buộc, tối đa 100 ký tự).</summary>
        public class CreateTableQrDto
        {
            [Required]
            [StringLength(100)]
            public string Label { get; set; }
        }

        /// <summary>QR bàn vừa tạo kèm ảnh PNG (dạng base64) để admin in/dán tại bàn — mở rộng từ <see cref="TableQrDto"/>.</summary>
        public class TableQrGeneratedDto : TableQrDto
        {
            /// <summary>PNG bytes as base64 (data URL built client-side).</summary>
            public string PngBase64 { get; set; }
        }
}
