using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Inventory
{
        /// <summary>
        /// Dự báo nguyên liệu cần chuẩn bị cho sáng mai: lượng nếp (kg) ước
        /// tính từ trung bình số suất bán cùng thứ trong 4 tuần gần nhất, quy
        /// đổi qua định mức <c>NepPerPortionGrams</c>. Màn admin dùng để chuẩn
        /// bị nguyên liệu, tránh thiếu hàng giờ cao điểm hoặc thừa gây lãng phí.
        /// </summary>
        public class IngredientForecastDto
        {
            /// <summary>The date being forecast (tomorrow morning, Vietnam date).</summary>
            public DateTime Date { get; set; }

            /// <summary>Tên thứ trong tuần của ngày dự báo (vd "Thứ Hai").</summary>
            public string WeekdayName { get; set; }

            /// <summary>Average portions sold on the same weekday over the last 4 weeks.</summary>
            public double AveragePortions { get; set; }

            /// <summary>Estimated nếp needed (kg).</summary>
            public double NepKg { get; set; }

            /// <summary>Định mức nếp (gam) cho 1 suất — dùng để quy đổi số suất dự báo thành kg cần chuẩn bị.</summary>
            public int NepPerPortionGrams { get; set; }

            /// <summary>Human-readable description of the heuristic.</summary>
            public string Heuristic { get; set; }
        }
}
