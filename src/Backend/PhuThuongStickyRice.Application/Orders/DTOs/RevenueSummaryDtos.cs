using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    /// <summary>
    /// Item 169: revenue summary of one Vietnam date range (cancelled orders
    /// excluded). The compare view calls this endpoint twice — once per
    /// period — so no compare-specific backend logic is needed.
    /// </summary>
    public class RevenueSummaryDto
    {
        /// <summary>Ngày bắt đầu kỳ báo cáo (giờ Việt Nam).</summary>
        public DateTime? From { get; set; }

        /// <summary>Ngày kết thúc kỳ báo cáo (giờ Việt Nam).</summary>
        public DateTime? To { get; set; }

        /// <summary>Tổng số đơn trong kỳ (đơn hủy không tính).</summary>
        public long OrderCount { get; set; }

        /// <summary>Tổng doanh thu trong kỳ (VNĐ).</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>Giá trị đơn trung bình = TotalRevenue / OrderCount.</summary>
        public decimal AvgOrderValue { get; set; }

        /// <summary>Top món bán chạy nhất theo doanh thu trong kỳ.</summary>
        public List<RevenueSummaryTopProductDto> TopProducts { get; set; }
            = new List<RevenueSummaryTopProductDto>();
    }

    /// <summary>Một món trong danh sách top bán chạy của báo cáo doanh thu.</summary>
    public class RevenueSummaryTopProductDto
    {
        /// <summary>Tên món.</summary>
        public string ProductName { get; set; }

        /// <summary>Tổng số suất bán ra trong kỳ.</summary>
        public long Quantity { get; set; }

        /// <summary>Doanh thu món này đóng góp trong kỳ (VNĐ).</summary>
        public decimal Revenue { get; set; }
    }
}
