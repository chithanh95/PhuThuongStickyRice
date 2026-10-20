using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Products.Models
{
    /// <summary>
    /// Kết quả tổng hợp trả về từ API import CSV sản phẩm: số món đã tạo,
    /// số dòng bỏ qua vì trùng mã và danh sách lỗi theo dòng — trang danh
    /// sách sản phẩm hiện thành alert tổng kết cho admin sau mỗi lần nhập.
    /// </summary>
    public class ProductImportResultModel
    {
        /// <summary>Số sản phẩm đã tạo mới thành công.</summary>
        public int Created { get; set; }

        /// <summary>Số dòng bị bỏ qua vì trùng mã với sản phẩm đã có.</summary>
        public int Skipped { get; set; }

        /// <summary>Danh sách dòng lỗi (số dòng + mã + lý do).</summary>
        public List<ProductImportErrorModel> Errors { get; set; } = new List<ProductImportErrorModel>();
    }

    /// <summary>Một dòng CSV import thất bại: số dòng trong file, mã món và lý do lỗi.</summary>
    public class ProductImportErrorModel
    {
        /// <summary>Số dòng trong file CSV (dòng 1 là header, dữ liệu từ dòng 2).</summary>
        public int Row { get; set; }

        /// <summary>Mã sản phẩm của dòng lỗi.</summary>
        public string Code { get; set; }

        /// <summary>Lý do lỗi do API trả về.</summary>
        public string Message { get; set; }
    }
}
