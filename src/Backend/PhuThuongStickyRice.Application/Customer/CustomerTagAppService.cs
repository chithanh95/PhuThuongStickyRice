using ClosedXML.Excel;
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

    public class CustomerTagAppService : ApplicationService, ICustomerTagAppService
    {
        private const int MaxExportRows = 5000;

        private readonly Volo.Abp.Domain.Repositories.IRepository<CustomerTag, Guid> _tags;
        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public CustomerTagAppService(
            Volo.Abp.Domain.Repositories.IRepository<CustomerTag, Guid> tags,
            IOrderRepository orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _tags = tags;
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

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

        public virtual async Task DeleteTagAsync(Guid id)
        {
            var entity = await _tags.GetAsync(id);
            await _tags.DeleteAsync(entity);
        }

        public virtual async Task<byte[]> ExportCustomersAsync(string searchTerm, string tag, bool inactiveOnly)
        {
            var rows = await GetCustomerRowsAsync(searchTerm, tag, inactiveOnly, MaxExportRows);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Khách hàng");

            var headers = new[] { "Tên khách hàng", "SĐT", "Tổng đơn", "Tổng chi tiêu (₫)", "Lần mua cuối", "Tags" };
            for (var i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
            }

            var headerRow = ws.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#7a6fbe");
            headerRow.Style.Font.FontColor = XLColor.White;

            var rowIndex = 2;
            foreach (var r in rows)
            {
                ws.Cell(rowIndex, 1).Value = r.CustomerName;
                ws.Cell(rowIndex, 2).Value = r.CustomerPhone;
                ws.Cell(rowIndex, 3).Value = r.OrderCount;
                ws.Cell(rowIndex, 4).Value = r.TotalSpent;
                ws.Cell(rowIndex, 4).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 5).Value = r.LastOrderAt.AddHours(7);
                ws.Cell(rowIndex, 5).Style.NumberFormat.Format = "dd/mm/yyyy hh:mm";
                ws.Cell(rowIndex, 6).Value = string.Join(", ", r.Tags.Select(t => t.TagName));
                rowIndex++;
            }

            ws.Columns().AdjustToContents();

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
                var cutoff = DateTime.UtcNow.AddDays(-30);
                grouped = grouped.Where(x => x.LastOrderAt < cutoff);
            }

            List<CustomerAggregateRow> customers;
            if (string.IsNullOrWhiteSpace(tag))
            {
                // No tag filter: stop at maxRows in SQL instead of materializing
                // every customer group then Take()ing in memory. Ordering kept
                // (TotalSpent desc) so the result is identical.
                customers = await _asyncExecuter.ToListAsync(
                    grouped.OrderByDescending(x => x.TotalSpent).Take(maxRows));
            }
            else
            {
                // Tag filter needs the tag rows first; keep the old order:
                // materialize groups, filter by tag in memory, then Take.
                customers = await _asyncExecuter.ToListAsync(
                    grouped.OrderByDescending(x => x.TotalSpent));
            }

            var phones = customers.Select(x => x.CustomerPhone).ToList();
            var tagsByPhone = await GetTagsByPhonesAsync(phones);

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var wanted = tag.Trim();
                customers = customers
                    .Where(x => tagsByPhone.TryGetValue(x.CustomerPhone, out var tags)
                        && tags.Any(t => string.Equals(t.TagName, wanted, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

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
