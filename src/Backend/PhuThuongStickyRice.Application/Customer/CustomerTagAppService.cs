using ClosedXML.Excel;
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Customer
{
    // [codequality-docs 2026-10-19]

    /// <summary>Hợp đồng dịch vụ tag khách hàng (gắn nhãn thủ công theo SĐT).</summary>
    public interface ICustomerTagAppService : IApplicationService
    {
        /// <summary>Admin/Cashier: tags for one phone number.</summary>
        Task<List<CustomerTagDto>> GetTagsAsync(string phone);

        /// <summary>Admin/Cashier: tags for many phone numbers (one lookup, no N+1).</summary>
        Task<Dictionary<string, List<CustomerTagDto>>> GetTagsByPhonesAsync(IEnumerable<string> phones);

        /// <summary>Admin/Cashier: add a tag to a customer phone.</summary>
        Task<CustomerTagDto> AddTagAsync(CreateCustomerTagDto dto);

        /// <summary>Admin/Cashier: remove a tag.</summary>
        Task DeleteTagAsync(Guid id);

        /// <summary>
        /// Admin/Cashier: customer list as .xlsx. Same filters as the admin
        /// customer page: searchTerm, tag, inactiveOnly (&gt;30 days).
        /// </summary>
        Task<byte[]> ExportCustomersAsync(string searchTerm, string tag, bool inactiveOnly);
    }

    /// <summary>
    /// Dịch vụ tag khách hàng: admin/cashier gắn nhãn thủ công cho SĐT khách hàng
    /// (VD: "khách quen", "khó tính"), tra cứu tag theo SĐT, và xuất file Excel
    /// danh sách khách hàng tổng hợp theo SĐT (trừ đơn đã hủy) kèm tổng chi tiêu.
    /// </summary>
    public class CustomerTagAppService : ApplicationService, ICustomerTagAppService
    {
        /// <summary>Số dòng tối đa của file Excel xuất khách hàng — giới hạn để không sinh file quá nặng.</summary>
        private const int MaxExportRows = 5000;

        /// <summary>Số ngày không mua thì coi là khách ngủ đông (inactive).</summary>
        private const int InactiveCutoffDays = 30;

        private readonly Volo.Abp.Domain.Repositories.IRepository<CustomerTag, Guid> _tags;
        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>Khởi tạo dịch vụ tag khách hàng.</summary>
        /// <param name="tags">Repository tag theo SĐT.</param>
        /// <param name="orders">Repository đơn hàng (để tổng hợp khách hàng xuất Excel).</param>
        /// <param name="asyncExecuter">Thực thi truy vấn async.</param>
        public CustomerTagAppService(
            Volo.Abp.Domain.Repositories.IRepository<CustomerTag, Guid> tags,
            IOrderRepository orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _tags = tags;
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>Lấy các tag của một SĐT (đã chuẩn hóa), sắp xếp theo tên.</summary>
        /// <param name="phone">SĐT khách hàng; rỗng thì trả về danh sách rỗng.</param>
        /// <returns>Danh sách tag.</returns>
        public virtual async Task<List<CustomerTagDto>> GetTagsAsync(string phone)
        {
            phone = NormalizePhone(phone);
            if (string.IsNullOrEmpty(phone))
            {
                return new List<CustomerTagDto>();
            }

            var queryable = await _tags.GetQueryableAsync();
            var rows = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.CustomerPhone == phone)
                    .OrderBy(x => x.TagName));
            return rows.Select(ToDto).ToList();
        }

        /// <summary>
        /// Lấy tag cho nhiều SĐT trong một lần gọi (tránh N+1). SĐT được chuẩn hóa,
        /// loại trùng; câu IN được chia batch vì SQL Server giới hạn số tham số.
        /// </summary>
        /// <param name="phones">Danh sách SĐT cần tra tag.</param>
        /// <returns>Dictionary SĐT → danh sách tag (SĐT không có tag không có key).</returns>
        public virtual async Task<Dictionary<string, List<CustomerTagDto>>> GetTagsByPhonesAsync(IEnumerable<string> phones)
        {
            var list = (phones ?? Enumerable.Empty<string>())
                .Select(NormalizePhone)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();
            var result = new Dictionary<string, List<CustomerTagDto>>();
            if (list.Count == 0)
            {
                return result;
            }

            var queryable = await _tags.GetQueryableAsync();

            // Chunk the IN clause (<= 1000 params/query; SQL Server allows ~2100).
            // Callers may pass thousands of phones (export tags lookup), which
            // would otherwise fail with "too many parameters" on SQL Server.
            var rows = new List<CustomerTag>();
            foreach (var batch in list.Chunk(1000))
            {
                rows.AddRange(await _asyncExecuter.ToListAsync(
                    queryable.Where(x => batch.Contains(x.CustomerPhone))));
            }
            foreach (var row in rows)
            {
                if (!result.TryGetValue(row.CustomerPhone, out var tags))
                {
                    tags = new List<CustomerTagDto>();
                    result[row.CustomerPhone] = tags;
                }

                tags.Add(ToDto(row));
            }

            return result;
        }

        /// <summary>
        /// Gắn tag mới cho SĐT: validate SĐT + tên tag, chặn trùng (cùng SĐT + tên tag),
        /// tự gán màu theo tên nếu admin không chọn màu.
        /// </summary>
        /// <param name="dto">SĐT, tên tag và màu.</param>
        /// <returns>Tag vừa tạo.</returns>
        public virtual async Task<CustomerTagDto> AddTagAsync(CreateCustomerTagDto dto)
        {
            var phone = NormalizePhone(dto?.CustomerPhone);
            var name = dto?.TagName?.Trim();
            if (string.IsNullOrEmpty(phone))
            {
                throw new UserFriendlyException("Vui lòng nhập số điện thoại.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new UserFriendlyException("Vui lòng nhập tên tag.");
            }

            if (name.Length > CustomerTag.TagNameMaxLength)
            {
                throw new UserFriendlyException($"Tên tag tối đa {CustomerTag.TagNameMaxLength} ký tự.");
            }

            var color = string.IsNullOrWhiteSpace(dto.Color)
                ? CustomerTagPresets.ColorFor(name)
                : dto.Color.Trim();

            var queryable = await _tags.GetQueryableAsync();
            var exists = await _asyncExecuter.AnyAsync(
                queryable.Where(x => x.CustomerPhone == phone && x.TagName == name));
            if (exists)
            {
                throw new UserFriendlyException("Tag này đã có cho khách hàng.");
            }

            var entity = new CustomerTag
            {
                CustomerPhone = phone,
                TagName = name,
                Color = color,
                CreatedBy = CurrentUser?.UserName,
                CreatedAt = DateTime.UtcNow,
            };
            await _tags.InsertAsync(entity);
            return ToDto(entity);
        }

        /// <summary>Xóa một tag theo id.</summary>
        /// <param name="id">Id của tag.</param>
        public virtual async Task DeleteTagAsync(Guid id)
        {
            var entity = await _tags.GetAsync(id);
            await _tags.DeleteAsync(entity);
        }

        /// <summary>
        /// Xuất danh sách khách hàng ra file .xlsx: mỗi SĐT một dòng gồm tên, tổng đơn,
        /// tổng chi tiêu, lần mua cuối (hiển thị giờ VN) và các tag. Dùng cùng bộ lọc
        /// với trang khách hàng admin; giới hạn ở <see cref="MaxExportRows"/> dòng.
        /// </summary>
        /// <param name="searchTerm">Tìm theo SĐT hoặc tên.</param>
        /// <param name="tag">Chỉ khách có tag này.</param>
        /// <param name="inactiveOnly">Chỉ khách ngủ đông (&gt;<see cref="InactiveCutoffDays"/> ngày không mua).</param>
        /// <returns>Nội dung file .xlsx.</returns>
        public virtual async Task<byte[]> ExportCustomersAsync(string searchTerm, string tag, bool inactiveOnly)
        {
            var rows = await GetCustomerRowsAsync(searchTerm, tag, inactiveOnly, MaxExportRows);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Khách hàng");

            var headers = new[] { "Tên khách hàng", "SĐT", "Tổng đơn", "Tổng chi tiêu (₫)", "Lần mua cuối", "Tags" };
            for (var i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#7a6fbe");
            headerRow.Style.Font.FontColor = XLColor.White;

            var rowIndex = 2;
            foreach (var r in rows)
            {
                worksheet.Cell(rowIndex, 1).Value = r.CustomerName;
                worksheet.Cell(rowIndex, 2).Value = r.CustomerPhone;
                worksheet.Cell(rowIndex, 3).Value = r.OrderCount;
                worksheet.Cell(rowIndex, 4).Value = r.TotalSpent;
                worksheet.Cell(rowIndex, 4).Style.NumberFormat.Format = "#,##0";
                // Lần mua cuối lưu ở DB là UTC — cộng offset giờ VN để hiển thị đúng giờ địa phương trong Excel.
                worksheet.Cell(rowIndex, 5).Value = r.LastOrderAt.AddHours(VietnamTime.UtcOffsetHours);
                worksheet.Cell(rowIndex, 5).Style.NumberFormat.Format = "dd/mm/yyyy hh:mm";
                worksheet.Cell(rowIndex, 6).Value = string.Join(", ", r.Tags.Select(t => t.TagName));
                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Customer rows aggregated by phone (same rule as the admin
        /// customer page: excludes cancelled orders), with tags attached.
        /// </summary>
        internal async Task<List<CustomerExportRow>> GetCustomerRowsAsync(
            string searchTerm, string tag, bool inactiveOnly, int maxRows)
        {
            var customers = await QueryCustomerAggregatesAsync(searchTerm, inactiveOnly, tag, maxRows);
            var tagsByPhone = await GetTagsByPhonesAsync(
                customers.Select(x => x.CustomerPhone).ToList());

            if (!string.IsNullOrWhiteSpace(tag))
            {
                customers = FilterByTag(customers, tagsByPhone, tag.Trim());
            }

            return ToExportRows(customers, tagsByPhone, maxRows);
        }

        /// <summary>
        /// Gom đơn (trừ đơn đã hủy) theo SĐT ngay trên SQL: lọc tìm kiếm và
        /// khách ngủ đông (&gt; <see cref="InactiveCutoffDays"/> ngày không mua).
        /// Cả hai nhánh (có/không filter tag) đều dừng ở maxRows ngay trên SQL.
        /// </summary>
        private async Task<List<CustomerAggregateRow>> QueryCustomerAggregatesAsync(
            string searchTerm, bool inactiveOnly, string tag, int maxRows)
        {
            var orderQueryable = await _orders.GetQueryableAsync();
            var query = orderQueryable.Where(x => x.Status != OrderStatus.Cancelled);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(x =>
                    x.CustomerPhone.Contains(term) || x.CustomerName.Contains(term));
            }

            // Grouped aggregation stays on SQL (same numbers as before).
            var grouped = query.GroupBy(x => x.CustomerPhone)
                .Select(g => new CustomerAggregateRow
                {
                    CustomerPhone = g.Key,
                    CustomerName = g.Max(x => x.CustomerName),
                    OrderCount = g.LongCount(),
                    TotalSpent = g.Sum(x => x.TotalAmount.Amount),
                    LastOrderAt = g.Max(x => x.CreatedAt),
                });

            // Push the inactive cutoff down to SQL (was an in-memory filter).
            if (inactiveOnly)
            {
                var cutoff = DateTime.UtcNow.AddDays(-InactiveCutoffDays);
                grouped = grouped.Where(x => x.LastOrderAt < cutoff);
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                grouped = await ApplyTagFilterAsync(grouped, tag);
            }

            // Xếp hạng + giới hạn gom ở cuối để mọi nhánh (không lọc / lọc
            // tag) dùng chung 1 câu order — thứ tự kết quả đồng nhất.
            return await _asyncExecuter.ToListAsync(
                grouped.OrderByDescending(x => x.TotalSpent).Take(maxRows));
        }

        /// <summary>
        /// Lọc nhóm khách theo tag admin gán. Tên tag so khớp không phân biệt
        /// hoa/thường; SĐT trong bảng tag đã chuẩn hóa lúc lưu nên chỉ khớp
        /// khi bằng SĐT thô trên đơn — đúng y hệt FilterByTag cũ.
        /// </summary>
        /// <remarks>
        /// Tag filter chạy trên SQL thay vì nạp toàn bộ nhóm khách hàng vào
        /// memory. Tên tag do admin tạo thủ công nên số lượng tên khác nhau
        /// rất nhỏ — so khớp tên trong memory (giữ đúng ngữ nghĩa
        /// OrdinalIgnoreCase trên mọi provider), rồi lọc nhóm khách bằng
        /// subquery EXISTS trên bảng tag.
        /// </remarks>
        private async Task<IQueryable<CustomerAggregateRow>> ApplyTagFilterAsync(
            IQueryable<CustomerAggregateRow> grouped, string tag)
        {
            var wanted = tag.Trim();
            var tagQueryable = await _tags.GetQueryableAsync();
            var distinctNames = await _asyncExecuter.ToListAsync(
                tagQueryable.Select(x => x.TagName).Distinct());
            var matchedNames = distinctNames
                .Where(n => string.Equals(n, wanted, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var tagPhones = tagQueryable
                .Where(x => matchedNames.Contains(x.TagName))
                .Select(x => x.CustomerPhone);

            return grouped.Where(x => tagPhones.Contains(x.CustomerPhone));
        }

        /// <summary>Giữ lại các dòng có tag trùng tên (không phân biệt hoa/thường).</summary>
        private static List<CustomerAggregateRow> FilterByTag(
            List<CustomerAggregateRow> customers,
            Dictionary<string, List<CustomerTagDto>> tagsByPhone,
            string wanted)
        {
            return customers
                .Where(x => tagsByPhone.TryGetValue(x.CustomerPhone, out var tags)
                    && tags.Any(t => string.Equals(t.TagName, wanted, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        /// <summary>Chốt maxRows rồi map sang row xuất Excel, gắn tag đã sắp xếp theo tên.</summary>
        private static List<CustomerExportRow> ToExportRows(
            List<CustomerAggregateRow> customers,
            Dictionary<string, List<CustomerTagDto>> tagsByPhone,
            int maxRows)
        {
            return customers
                .Take(maxRows)
                .Select(x => new CustomerExportRow
                {
                    CustomerName = x.CustomerName,
                    CustomerPhone = x.CustomerPhone,
                    OrderCount = x.OrderCount,
                    TotalSpent = x.TotalSpent,
                    LastOrderAt = x.LastOrderAt,
                    Tags = tagsByPhone.TryGetValue(x.CustomerPhone, out var tags)
                        ? tags.OrderBy(t => t.TagName).ToList()
                        : new List<CustomerTagDto>(),
                })
                .ToList();
        }

        private static string NormalizePhone(string phone) =>
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        private static CustomerTagDto ToDto(CustomerTag x) => new CustomerTagDto
        {
            Id = x.Id,
            CustomerPhone = x.CustomerPhone,
            TagName = x.TagName,
            Color = x.Color,
            CreatedBy = x.CreatedBy,
            CreatedAt = x.CreatedAt,
        };

        /// <summary>Một dòng dữ liệu của file Excel xuất khách hàng (đã tổng hợp theo SĐT).</summary>
        internal class CustomerExportRow
        {
            public string CustomerName { get; set; }

            public string CustomerPhone { get; set; }

            public long OrderCount { get; set; }

            public decimal TotalSpent { get; set; }

            public DateTime LastOrderAt { get; set; }

            public List<CustomerTagDto> Tags { get; set; }
        }

        /// <summary>
        /// One aggregated row per customer phone (same rule as the admin
        /// customer page: excludes cancelled orders).
        /// </summary>
        internal class CustomerAggregateRow
        {
            public string CustomerPhone { get; set; }

            public string CustomerName { get; set; }

            public long OrderCount { get; set; }

            public decimal TotalSpent { get; set; }

            public DateTime LastOrderAt { get; set; }
        }
    }
}
