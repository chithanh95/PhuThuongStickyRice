using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Staff
{
    public class StaffActivityLogDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string ActionLabel { get; set; }
        public string Detail { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GetStaffActivityLogsInput
    {
        public Guid? UserId { get; set; }
        public string Action { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int SkipCount { get; set; }
        public int MaxResultCount { get; set; } = 50;
    }

    public class StaffActivityLogListDto
    {
        public long TotalCount { get; set; }
        public List<StaffActivityLogDto> Items { get; set; } = new List<StaffActivityLogDto>();
        public List<string> Actions { get; set; } = new List<string>();
    }
}
