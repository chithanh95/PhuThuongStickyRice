using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Inventory
{
        public class IngredientForecastDto
        {
            /// <summary>The date being forecast (tomorrow morning, Vietnam date).</summary>
            public DateTime Date { get; set; }

            public string WeekdayName { get; set; }

            /// <summary>Average portions sold on the same weekday over the last 4 weeks.</summary>
            public double AveragePortions { get; set; }

            /// <summary>Estimated nếp needed (kg).</summary>
            public double NepKg { get; set; }

            public int NepPerPortionGrams { get; set; }

            /// <summary>Human-readable description of the heuristic.</summary>
            public string Heuristic { get; set; }
        }
}
