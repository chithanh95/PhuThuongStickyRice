using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Common
{
        /// <summary>
        /// Trạng thái bảo trì hệ thống: khi bật, web hiển thị trang
        /// "đang bảo trì" cho khách thay vì nhận đơn.
        /// </summary>
        public class SystemStatusDto
        {
            public bool MaintenanceMode { get; set; }
            public string MaintenanceMessage { get; set; }
        }

        /// <summary>
        /// Thống kê dung lượng ổ đĩa (file upload + database) để admin giám sát.
        /// Cảnh báo (OverThreshold) khi vượt ngưỡng WarningThresholdMb,
        /// tránh đầy ổ đĩa làm web ngừng hoạt động.
        /// </summary>
        public class DiskUsageDto
        {
            public long UploadsBytes { get; set; }
            public int UploadsFileCount { get; set; }
            public long? DatabaseBytes { get; set; }
            public int WarningThresholdMb { get; set; }
            public bool OverThreshold { get; set; }
        }
}
