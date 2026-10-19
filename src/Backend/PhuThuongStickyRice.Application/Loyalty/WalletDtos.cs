using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Loyalty
{
        /// <summary>
        /// Số dư ví của một khách hàng. Ví định danh bằng SĐT (không cần tài khoản đăng nhập).
        /// </summary>
        public class WalletAccountDto
        {
            /// <summary>Id bản ghi ví.</summary>
            public Guid Id { get; set; }

            /// <summary>SĐT chủ ví (đã chuẩn hóa đầu số Việt Nam).</summary>
            public string Phone { get; set; }

            /// <summary>Số dư hiện tại (VNĐ), không bao giờ âm.</summary>
            public decimal Balance { get; set; }
        }

        /// <summary>
        /// Một biến động số dư ví. Mọi lần trừ/hoàn/điều chỉnh đều ghi lại một dòng này
        /// để đối soát và chống hoàn tiền trùng lặp.
        /// </summary>
        public class WalletTransactionDto
        {
            /// <summary>Id giao dịch.</summary>
            public Guid Id { get; set; }

            /// <summary>SĐT chủ ví.</summary>
            public string Phone { get; set; }

            /// <summary>Số tiền biến động: âm khi trừ (thanh toán đơn), dương khi cộng (nạp/hoàn).</summary>
            public decimal Amount { get; set; }

            /// <summary>Lý do ghi nhận (vd "Thanh toán đơn PX-...", "Hoàn tiền hủy đơn ...").</summary>
            public string Reason { get; set; }

            /// <summary>Số dư sau khi áp dụng giao dịch này.</summary>
            public decimal BalanceAfter { get; set; }

            /// <summary>Người thực hiện (email nhân viên / "system" cho luồng tự động).</summary>
            public string CreatedBy { get; set; }

            /// <summary>Thời điểm giao dịch (UTC).</summary>
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// Input admin nạp/trừ tay tiền ví: số dương = nạp tiền, số âm = trừ tay.
        /// </summary>
        public class AdjustWalletInput
        {
            /// <summary>SĐT chủ ví cần điều chỉnh.</summary>
            [Required]
            [StringLength(30)]
            public string Phone { get; set; }

            /// <summary>Số dương = nạp tiền, số âm = trừ tay. Phải khác 0 và nằm trong 1.000đ – 100.000.000đ.</summary>
            [Range(1000, 100000000)]
            public decimal Amount { get; set; }

            /// <summary>Lý do điều chỉnh (ghi vào lịch sử giao dịch để đối soát).</summary>
            [Required]
            [StringLength(500)]
            public string Reason { get; set; }
        }
}
