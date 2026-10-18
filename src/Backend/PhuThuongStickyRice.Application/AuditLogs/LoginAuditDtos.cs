using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.AuditLogs
{
        public class LoginAuditDto
        {
            public Guid Id { get; set; }

            public string UserName { get; set; }

            public DateTime At { get; set; }

            public string IpAddress { get; set; }

            public bool Success { get; set; }

            public string FailureReason { get; set; }
        }
}
