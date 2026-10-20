using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Reports
{
        /// <summary>
        /// Số liệu so sánh khách hàng mới và khách hàng quay lại trong một khoảng thời gian.
        /// Dùng cho báo cáo chủ quán và dashboard (biểu đồ khách mới/quay lại).
        /// </summary>
        public class NewAndReturningCustomersDto
        {
            public long NewCustomers { get; set; }

            public long ReturningCustomers { get; set; }

            public long NewCustomerOrders { get; set; }

            public long ReturningCustomerOrders { get; set; }
        }

        /// <summary>
        /// Kết quả gửi báo cáo chủ quán (Zalo OA): khoảng thời gian báo cáo,
        /// đã gửi thành công hay chưa, và lý do khi chưa gửi được (vd chưa
        /// cấu hình token Zalo — lúc đó trả nội dung text để copy thủ công).
        /// </summary>
        public class OwnerReportResultDto
        {
            public string Period { get; set; }

            public bool IsSent { get; set; }

            public string Reason { get; set; }
        }
}
