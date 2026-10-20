using System.Collections.Generic;

namespace PhuThuongStickyRice.WebAPI.Models.Products
{
    /// <summary>
    /// Kết quả tổng hợp của 1 lần import CSV sản phẩm: số dòng đã tạo mới,
    /// số dòng bỏ qua vì trùng mã và danh sách lỗi theo dòng. Trang admin
    /// dùng để hiện tổng kết cho người nhập thay vì danh sách sản phẩm
    /// như bản preview cũ (chưa ghi DB).
    /// </summary>
    public class ProductImportResultModel
    {
        /// <summary>Số sản phẩm đã tạo mới thành công.</summary>
        public int Created { get; set; }

        /// <summary>Số dòng bị bỏ qua vì trùng mã với sản phẩm đã có (không ghi đè).</summary>
        public int Skipped { get; set; }

        /// <summary>Danh sách dòng lỗi (số dòng + mã + lý do), theo thứ tự xuất hiện trong file.</summary>
        public List<ProductImportErrorModel> Errors { get; set; } = new List<ProductImportErrorModel>();
    }

    /// <summary>Một dòng CSV import thất bại: số dòng trong file, mã món và lý do lỗi.</summary>
    public class ProductImportErrorModel
    {
        /// <summary>Số dòng trong file CSV (dòng 1 là header, dữ liệu bắt đầu từ dòng 2).</summary>
        public int Row { get; set; }

        /// <summary>Mã sản phẩm của dòng lỗi (rỗng nếu chính dòng đó thiếu mã).</summary>
        public string Code { get; set; }

        /// <summary>Lý do lỗi — message của exception khi tạo sản phẩm từ dòng này.</summary>
        public string Message { get; set; }
    }
}
