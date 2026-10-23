using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Admin.Models
{
    /// <summary>
    /// Dữ liệu gửi lên POST api/pos/orders: tạo đơn bán tại quầy cho khách
    /// mua trực tiếp. Tên/SĐT để trống thì server tự gán "Khách vãng lai" /
    /// SĐT placeholder 0000000000 (PosAppService phía backend).
    /// </summary>
    public class CreatePosOrderModel
    {
        /// <summary>Tên khách (tùy chọn).</summary>
        public string CustomerName { get; set; }

        /// <summary>SĐT khách (tùy chọn — có SĐT thì đơn được tích điểm như đơn web).</summary>
        public string CustomerPhone { get; set; }

        /// <summary>Ghi chú đơn (tùy chọn).</summary>
        public string Note { get; set; }

        /// <summary>Các dòng món trong đơn; bắt buộc có ít nhất 1 dòng.</summary>
        public List<PosOrderLineModel> Lines { get; set; } = new List<PosOrderLineModel>();
    }

    /// <summary>Một dòng món trong đơn POS.</summary>
    public class PosOrderLineModel
    {
        /// <summary>Món được chọn.</summary>
        public Guid ProductId { get; set; }

        /// <summary>Số lượng (&gt;= 1).</summary>
        public int Quantity { get; set; }

        /// <summary>Kích cỡ đã chọn; null khi món không có kích cỡ.</summary>
        public Guid? SizeOptionId { get; set; }

        /// <summary>Các topping/món ăn kèm đã chọn.</summary>
        public List<Guid> ToppingIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// Kết quả trả về sau khi tạo đơn POS — trích từ OrderDto của server:
    /// chỉ các trường màn hình POS cần để hiện xác nhận (mã đơn, tổng tiền,
    /// trạng thái). Đơn POS hoàn tất + đã thanh toán ngay khi tạo xong.
    /// </summary>
    public class PosOrderResultModel
    {
        /// <summary>Id đơn (dùng cho nút in hóa đơn /orders/print/{id}).</summary>
        public Guid Id { get; set; }

        /// <summary>Mã đơn hiển thị cho khách (vd PX-20261023-1234).</summary>
        public string OrderNumber { get; set; }

        /// <summary>Tổng tiền server đã tính (nguồn sự thật về tiền).</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Trạng thái đơn sau khi tạo (kỳ vọng Completed).</summary>
        public string Status { get; set; }

        /// <summary>Trạng thái thanh toán (kỳ vọng Paid — tiền mặt tại quầy).</summary>
        public string PaymentStatus { get; set; }
    }
}
