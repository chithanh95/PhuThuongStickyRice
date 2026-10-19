using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Common
{
        // codequality-team 2026-10-20 run-0245 Agent4-Docs
        /// <summary>
        /// Trạng thái bảo trì hệ thống: khi bật, web hiển thị trang
        /// "đang bảo trì" cho khách thay vì nhận đơn.
        /// </summary>
        public class SystemStatusDto
        {
            /// <summary>Bật chế độ bảo trì (chặn nhận đơn mới).</summary>
            public bool MaintenanceMode { get; set; }
            /// <summary>Thông báo hiển thị cho khách khi đang bảo trì.</summary>
            public string MaintenanceMessage { get; set; }
        }

        /// <summary>
        /// Thống kê dung lượng ổ đĩa (file upload + database) để admin giám sát.
        /// Cảnh báo (OverThreshold) khi vượt ngưỡng WarningThresholdMb,
        /// tránh đầy ổ đĩa làm web ngừng hoạt động.
        /// </summary>
        public class DiskUsageDto
        {
            /// <summary>Tổng dung lượng file upload (byte).</summary>
            public long UploadsBytes { get; set; }
            /// <summary>Số lượng file trong thư mục upload.</summary>
            public int UploadsFileCount { get; set; }
            /// <summary>Dung lượng database (byte); null khi DB không báo cáo được kích thước.</summary>
            public long? DatabaseBytes { get; set; }
            /// <summary>Ngưỡng cảnh báo (MB) do admin cấu hình.</summary>
            public int WarningThresholdMb { get; set; }
            /// <summary>True khi tổng dung lượng đã vượt ngưỡng cảnh báo.</summary>
            public bool OverThreshold { get; set; }
        }
}
