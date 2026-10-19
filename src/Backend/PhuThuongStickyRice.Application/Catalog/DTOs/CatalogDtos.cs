using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace PhuThuongStickyRice.Application.Catalog.DTOs
{
    /// <summary>
    /// Topping (món ăn kèm, vd thêm ruốc/hành phi) hiển thị ở shop: tên,
    /// phụ phí (VNĐ) cộng vào giá món, thứ tự sắp xếp và trạng thái bật/tắt.
    /// </summary>
    public class ToppingDto : EntityDto<Guid>
    {
        public string Name { get; set; }

        public decimal ExtraPrice { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    /// <summary>Input admin tạo/sửa topping: tên (bắt buộc), phụ phí (VNĐ, ≥ 0), thứ tự hiển thị.</summary>
    public class CreateUpdateToppingDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ExtraPrice { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>Một món trong combo: sản phẩm, giá tại thời điểm tạo combo (VNĐ) và số lượng trong gói.</summary>
    public class ComboItemDto
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal ProductPrice { get; set; }

        public int Quantity { get; set; }
    }

    /// <summary>
    /// Gói combo (vd combo 5 phần xôi) bán ở shop: giá gói ưu đãi (VNĐ),
    /// danh sách món trong gói và trạng thái bán.
    /// </summary>
    public class ComboDto : EntityDto<Guid>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        public List<ComboItemDto> Items { get; set; } = new List<ComboItemDto>();
    }

    /// <summary>Một món trong input tạo/sửa combo: Id sản phẩm (bắt buộc) và số lượng (≥ 1).</summary>
    public class CreateUpdateComboItemDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    /// <summary>Input admin tạo/sửa combo: tên (bắt buộc), mô tả, giá gói ưu đãi (VNĐ, ≥ 0) và danh sách món.</summary>
    public class CreateUpdateComboDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public List<CreateUpdateComboItemDto> Items { get; set; } = new List<CreateUpdateComboItemDto>();
    }

    /// <summary>
    /// Yêu cầu báo giá số lượng lớn (đặt cỗ/cúng): thông tin khách, ngày tổ
    /// chức, số lượng (mặc định 10), ngân sách, địa điểm và trạng thái xử lý
    /// (mới/đã duyệt/đã từ chối) để admin theo dõi.
    /// </summary>
    public class QuoteRequestDto : EntityDto<Guid>
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public DateTime? EventDate { get; set; }

        public int Quantity { get; set; }

        public string Notes { get; set; }

        public string EventLocation { get; set; }

        public string Budget { get; set; }

        public string QuoteType { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>Item 161: số tiền cọc khi duyệt (VND). Null = chưa yêu cầu cọc.</summary>
        public decimal? DepositAmount { get; set; }

        /// <summary>Item 161: thông tin chuyển khoản gửi khách.</summary>
        public string DepositInfo { get; set; }
    }

    /// <summary>Input khách gửi yêu cầu báo giá số lượng lớn (trang "đặt số lượng lớn").</summary>
    public class CreateQuoteRequestDto
    {
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(30)]
        public string CustomerPhone { get; set; }

        public DateTime? EventDate { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 10;

        [StringLength(1000)]
        public string Notes { get; set; }

        [StringLength(500)]
        public string EventLocation { get; set; }

        [StringLength(100)]
        public string Budget { get; set; }

        [StringLength(50)]
        public string QuoteType { get; set; }
    }

    /// <summary>Monthly xoi-cung reminder subscription (Rằm / mùng 1).</summary>
    public class CungReminderDto : EntityDto<Guid>
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>"Ram" or "Mung1".</summary>
        public string ReminderType { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Input đăng ký nhắc cúng xôi Rằm/mùng 1: tên, SĐT và loại nhắc ("Ram"/"Mung1").</summary>
    public class CreateCungReminderDto
    {
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(30)]
        public string CustomerPhone { get; set; }

        /// <summary>"Ram" (rằm) or "Mung1" (mùng 1).</summary>
        [Required]
        public string ReminderType { get; set; }
    }

    /// <summary>Input admin đổi trạng thái xử lý của một yêu cầu báo giá.</summary>
    public class UpdateQuoteRequestStatusDto
    {
        [Required]
        public string Status { get; set; }
    }

    /// <summary>Item 161: admin đặt số tiền cọc khi duyệt báo giá.</summary>
    public class SetQuoteDepositDto
    {
        [Range(0, double.MaxValue)]
        public decimal DepositAmount { get; set; }

        [StringLength(500)]
        public string DepositInfo { get; set; }
    }

    /// <summary>Input admin lọc yêu cầu báo giá: tìm kiếm theo tên/SĐT và lọc theo trạng thái.</summary>
    public class GetQuoteRequestsInput : PagedAndSortedResultRequestDto
    {
        public string SearchTerm { get; set; }

        public string Status { get; set; }
    }
}
