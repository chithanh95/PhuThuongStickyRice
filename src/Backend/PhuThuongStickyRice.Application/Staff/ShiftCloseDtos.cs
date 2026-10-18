using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Staff
{
        public class ShiftCloseDto
        {
            public Guid Id { get; set; }
            public DateTime Date { get; set; }
            public decimal ExpectedCod { get; set; }
            public decimal ActualCod { get; set; }

            /// <summary>ActualCod - ExpectedCod.</summary>
            public decimal Discrepancy { get; set; }

            public string Note { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class CloseShiftInput
        {
            public DateTime Date { get; set; }

            [Range(0, 1000000000)]
            public decimal ActualCod { get; set; }

            [StringLength(500)]
            public string Note { get; set; }
        }
}
