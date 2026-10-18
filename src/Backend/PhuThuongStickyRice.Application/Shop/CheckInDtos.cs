using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Shop
{
        public class CheckInResultDto
        {
            /// <summary>True when the phone already checked in today (no new points).</summary>
            public bool AlreadyCheckedIn { get; set; }

            /// <summary>Points awarded by this check-in (0 when already checked in).</summary>
            public int PointsAwarded { get; set; }

            /// <summary>Current consecutive-day streak including today.</summary>
            public int StreakDays { get; set; }

            public int LoyaltyPoints { get; set; }
        }
    public class CheckInRequestModel
    {
        public string Phone { get; set; }
    }
}
