using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    /// <summary>
    /// One day of historical revenue (Vietnam local date). Zero-revenue
    /// days are included so moving averages stay honest.
    /// </summary>
    public class ForecastHistoryPointDto
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }

        public long OrderCount { get; set; }

        public long Portions { get; set; }
    }

    /// <summary>
    /// Average daily performance of one weekday (Thứ 2..Chủ nhật),
    /// computed over the lookback window.
    /// </summary>
    public class WeekdayPatternDto
    {
        public DayOfWeek DayOfWeek { get; set; }

        public string WeekdayName { get; set; }

        /// <summary>How many occurrences of this weekday had data.</summary>
        public int Occurrences { get; set; }

        public decimal AvgRevenue { get; set; }

        public double AvgOrders { get; set; }

        public double AvgPortions { get; set; }

        /// <summary>
        /// AvgRevenue / overall daily average. 2.0 means "twice as busy as
        /// an average day".
        /// </summary>
        public double RatioVsAverage { get; set; }
    }

    /// <summary>Forecast for a single future day.</summary>
    public class ForecastDayDto
    {
        public DateTime Date { get; set; }

        public string WeekdayName { get; set; }

        public decimal ForecastRevenue { get; set; }

        public double ForecastOrders { get; set; }

        /// <summary>Rounded-up portions the shop should prepare.</summary>
        public long SuggestedPortions { get; set; }

        /// <summary>True when the weekday pattern was used; false = overall average fallback.</summary>
        public bool BasedOnWeekdayPattern { get; set; }
    }

    /// <summary>
    /// Tổng dự báo cho một khoảng tương lai (7 hoặc 30 ngày tới): tổng tiền
    /// dự kiến, số đơn dự kiến và số suất xôi quán nên chuẩn bị.
    /// </summary>
    public class ForecastTotalDto
    {
        /// <summary>Tổng doanh thu dự kiến (VNĐ) cho cả khoảng — chỉ là ước tính, không đảm bảo.</summary>
        public decimal ForecastRevenue { get; set; }

        /// <summary>Số đơn dự kiến cho cả khoảng (có thể lẻ vì là trung bình cộng).</summary>
        public double ForecastOrders { get; set; }

        /// <summary>Số suất xôi quán nên chuẩn bị thêm cho cả khoảng.</summary>
        public long SuggestedPortions { get; set; }
    }

    /// <summary>
    /// Revenue forecast for the shop. Heuristic, deliberately simple:
    /// per-weekday averages over recent history, falling back to the
    /// 30-day overall daily average when a weekday has too little data.
    /// This is an estimate, not a guarantee.
    /// </summary>
    public class RevenueForecastDto
    {
        /// <summary>Thời điểm tính dự báo (UTC) — dùng để biết số liệu còn mới hay đã cũ.</summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>Lookback window in days used for the pattern.</summary>
        public int HistoryDays { get; set; }

        /// <summary>How many of those days actually had orders.</summary>
        public int DaysWithOrders { get; set; }

        /// <summary>Doanh thu trung bình/ngày trong 7 ngày gần nhất (VNĐ).</summary>
        public decimal AvgDailyRevenue7d { get; set; }

        /// <summary>Doanh thu trung bình/ngày trong 30 ngày gần nhất (VNĐ) — fallback khi mẫu theo thứ trong tuần quá ít.</summary>
        public decimal AvgDailyRevenue30d { get; set; }

        /// <summary>Doanh thu thực tế từng ngày trong cửa sổ nhìn lại, để admin đối chiếu.</summary>
        public List<ForecastHistoryPointDto> History { get; set; } = new List<ForecastHistoryPointDto>();

        /// <summary>Mẫu doanh thu trung bình theo từng thứ trong tuần (vd: Thứ 7 bán tốt hơn).</summary>
        public List<WeekdayPatternDto> WeekdayPattern { get; set; } = new List<WeekdayPatternDto>();

        /// <summary>Dự báo chi tiết từng ngày trong 7 ngày tới.</summary>
        public List<ForecastDayDto> Next7Days { get; set; } = new List<ForecastDayDto>();

        /// <summary>Tổng dự báo 7 ngày tới (tiền, số đơn, suất xôi nên chuẩn bị).</summary>
        public ForecastTotalDto Next7DaysTotal { get; set; } = new ForecastTotalDto();

        /// <summary>Tổng dự báo 30 ngày tới (tiền, số đơn, suất xôi nên chuẩn bị).</summary>
        public ForecastTotalDto Next30DaysTotal { get; set; } = new ForecastTotalDto();

        /// <summary>Ghi chú phương pháp đang dùng (mẫu theo thứ trong tuần hay trung bình chung 30 ngày) — để admin hiểu con số đến từ đâu.</summary>
        public string MethodNote { get; set; }

        /// <summary>Câu cảnh báo đây chỉ là ước tính heuristic đơn giản, không phải cam kết doanh thu.</summary>
        public string Disclaimer { get; set; }
    }
}
