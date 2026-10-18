using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Common
{
        public class SystemStatusDto
        {
            public bool MaintenanceMode { get; set; }
            public string MaintenanceMessage { get; set; }
        }

        public class DiskUsageDto
        {
            public long UploadsBytes { get; set; }
            public int UploadsFileCount { get; set; }
            public long? DatabaseBytes { get; set; }
            public int WarningThresholdMb { get; set; }
            public bool OverThreshold { get; set; }
        }
}
