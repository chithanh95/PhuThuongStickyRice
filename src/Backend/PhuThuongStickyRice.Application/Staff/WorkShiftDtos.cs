using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Staff
{
        public class WorkShiftDto
        {
            public Guid Id { get; set; }
            public string StaffName { get; set; }
            public DateTime Date { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public string Note { get; set; }
        }

        public class CreateUpdateWorkShiftDto
        {
            [Required]
            [StringLength(100)]
            public string StaffName { get; set; }

            public DateTime Date { get; set; }

            public TimeSpan StartTime { get; set; }

            public TimeSpan EndTime { get; set; }

            [StringLength(500)]
            public string Note { get; set; }
        }
}
