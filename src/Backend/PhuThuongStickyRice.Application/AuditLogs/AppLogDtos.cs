using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.AuditLogs
{
        public class AppLogDto
        {
            public Guid Id { get; set; }
            public DateTime At { get; set; }
            public string Level { get; set; }
            public string Source { get; set; }
            public string Message { get; set; }
        }

        public class GetAppLogsInput
        {
            public string Level { get; set; }
            public string SearchTerm { get; set; }
            public int SkipCount { get; set; }
            public int MaxResultCount { get; set; } = 50;
        }
}
