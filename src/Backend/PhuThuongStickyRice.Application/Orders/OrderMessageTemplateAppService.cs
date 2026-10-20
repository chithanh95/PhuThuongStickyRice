using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Mẫu tin nhắn theo trạng thái đơn (trả về cho trang quản trị Admin).
    /// </summary>
    public class OrderMessageTemplateDto
    {
        /// <summary>Id mẫu tin nhắn.</summary>
        public Guid Id { get; set; }

        /// <summary>Mã trạng thái đơn (Confirmed/Preparing/Completed/Cancelled).</summary>
        public string Status { get; set; }

        /// <summary>Tên trạng thái hiển thị tiếng Việt (vd: "Đã xác nhận").</summary>
        public string StatusLabel { get; set; }

        /// <summary>
        /// Nội dung mẫu với các placeholder {MaDon}, {TenKhach}, {TongTien}, {NgayDat}.
        /// </summary>
        public string Template { get; set; }

        /// <summary>true = mẫu đang dùng; false = bỏ qua khi render tin nhắn.</summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Dữ liệu Admin nhập khi cập nhật mẫu tin nhắn.
    /// </summary>
    public class CreateUpdateOrderMessageTemplateDto
    {
        /// <summary>Nội dung mẫu tin nhắn (không được trống, tối đa TemplateMaxLength ký tự).</summary>
        public string Template { get; set; }

        /// <summary>Bật/tắt mẫu tin nhắn này.</summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Tin nhắn đã render sẵn từ mẫu + dữ liệu đơn (cho nút "📋 Copy tin nhắn").
    /// </summary>
    public class OrderMessagePreviewDto
    {
        /// <summary>Mã trạng thái đơn đã render.</summary>
        public string Status { get; set; }

        /// <summary>Tên trạng thái hiển thị tiếng Việt.</summary>
        public string StatusLabel { get; set; }

        /// <summary>
        /// Tin nhắn hoàn chỉnh (null nếu trạng thái chưa có mẫu đang bật).
        /// </summary>
        public string Message { get; set; }

        /// <summary>true = có mẫu đang bật cho trạng thái này.</summary>
        public bool HasTemplate { get; set; }
    }

    /// <summary>
    /// Mẫu tin nhắn chăm sóc khách theo trạng thái đơn: admin soạn sẵn nội dung
    /// (có placeholder {MaDon}, {TenKhach}, {TongTien}, {NgayDat}), nút
    /// "📋 Copy tin nhắn" ở trang đơn hàng render mẫu với dữ liệu đơn thật để
    /// nhân viên gửi Zalo/tin nhắn — hệ thống KHÔNG tự gửi gì cả.
    /// </summary>
    public interface IOrderMessageTemplateAppService : IApplicationService
    {
        /// <summary>Admin: all templates (one per status).</summary>
        Task<List<OrderMessageTemplateDto>> GetListAsync();

        /// <summary>Admin: update a template's text / active flag.</summary>
        Task<OrderMessageTemplateDto> UpdateAsync(Guid id, CreateUpdateOrderMessageTemplateDto dto);

        /// <summary>
        /// Admin/Cashier: renders the active template for a status with an
        /// order's data. Used by the "📋 Copy tin nhắn" button — the message
        /// is copied to the clipboard; nothing is sent automatically.
        /// </summary>
        Task<OrderMessagePreviewDto> GetPreviewAsync(Guid orderId, string status);
    }

    /// <summary>
    /// Item 162: message templates per order status. Templates are seeded
    /// for Confirmed/Preparing/Completed/Cancelled; staff copies the
    /// rendered message and sends it manually (Zalo/SMS) — honest flow,
    /// no pretend auto-send.
    /// </summary>
    public class OrderMessageTemplateAppService : ApplicationService, IOrderMessageTemplateAppService
    {
        /// <summary>Repository bảng OrderMessageTemplate (mẫu tin nhắn theo trạng thái).</summary>
        private readonly IRepository<OrderMessageTemplate, Guid> _templates;

        /// <summary>Repository bảng Order (đơn hàng — lấy dữ liệu để render mẫu).</summary>
        private readonly IRepository<Order, Guid> _orders;

        /// <summary>Thực thi query async trên IQueryable (ABP).</summary>
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>
        /// Số mẫu tin nhắn tối đa lấy ở server cho danh sách admin —
        /// bảng mẫu tin nhắn tăng dần theo cấu hình, không bao giờ
        /// materialize toàn bộ; đặt tên hằng số thay vì hardcode 500
        /// trong truy vấn (đồng bộ với MessageTemplateAppService).
        /// </summary>
        private const int MaxListRows = 500;

        /// <summary>Khởi tạo service mẫu tin nhắn theo trạng thái đơn.</summary>
        /// <param name="templates">Repository bảng OrderMessageTemplate.</param>
        /// <param name="orders">Repository bảng Order.</param>
        /// <param name="asyncExecuter">Executor chạy query LINQ async.</param>
        public OrderMessageTemplateAppService(
            IRepository<OrderMessageTemplate, Guid> templates,
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _templates = templates;
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Chuyển mã trạng thái đơn sang nhãn tiếng Việt hiển thị cho nhân viên/khách.
        /// </summary>
        /// <param name="status">Mã trạng thái (vd: "Confirmed").</param>
        /// <returns>Nhãn tiếng Việt; giữ nguyên mã nếu không nhận diện được.</returns>
        public static string StatusLabel(string status) => status switch
        {
            "Confirmed" => "Đã xác nhận",
            "Preparing" => "Đang chuẩn bị",
            "Completed" => "Hoàn thành",
            "Cancelled" => "Đã hủy",
            _ => status
        };

        [Authorize(Roles = "Admin")]
        /// <summary>
        /// Admin: lấy tất cả mẫu tin nhắn (mỗi trạng thái đơn một mẫu).
        /// </summary>
        /// <returns>Danh sách mẫu tin nhắn sắp xếp theo mã trạng thái.</returns>
        public virtual async Task<List<OrderMessageTemplateDto>> GetListAsync()
        {
            var queryable = await _templates.GetQueryableAsync();
            // perf-hunt 2026-10-19 (run 15:45): Take(MaxListRows) server-side
            // — bảng mẫu tin nhắn tăng dần theo cấu hình, không bao giờ nạp
            // toàn bộ. [codequality-practices 2026-10-20] magic number 500 →
            // hằng số có tên MaxListRows (đồng bộ các service template khác).
            var list = await _asyncExecuter.ToListAsync(queryable.OrderBy(x => x.Status).Take(MaxListRows));
            return list.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        /// <summary>
        /// Admin: cập nhật nội dung/cờ bật tắt của một mẫu tin nhắn.
        /// Nội dung phải khác trống và không quá TemplateMaxLength ký tự.
        /// </summary>
        /// <param name="id">Id mẫu tin nhắn.</param>
        /// <param name="dto">Nội dung mới và trạng thái bật/tắt.</param>
        /// <returns>Mẫu tin nhắn sau khi cập nhật.</returns>
        public virtual async Task<OrderMessageTemplateDto> UpdateAsync(Guid id, CreateUpdateOrderMessageTemplateDto dto)
        {
            var entity = await _templates.GetAsync(id);

            var template = dto?.Template?.Trim();
            if (string.IsNullOrWhiteSpace(template))
            {
                throw new Domain.Exceptions.DomainException("Nội dung mẫu tin nhắn không được để trống.");
            }

            if (template.Length > OrderMessageTemplate.TemplateMaxLength)
            {
                throw new Domain.Exceptions.DomainException(
                    $"Mẫu tin nhắn không được vượt quá {OrderMessageTemplate.TemplateMaxLength} ký tự.");
            }

            entity.Template = template;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await _templates.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin,Cashier")]
        /// <summary>
        /// Admin/Thu ngân: render mẫu tin nhắn đang bật của một trạng thái với
        /// dữ liệu đơn cụ thể (cho nút "📋 Copy tin nhắn"). Nếu trạng thái truyền
        /// vào không hợp lệ, dùng luôn trạng thái hiện tại của đơn.
        /// </summary>
        /// <param name="orderId">Id đơn hàng lấy dữ liệu (tên khách, tổng tiền...).</param>
        /// <param name="status">Mã trạng thái cần render; trống thì dùng trạng thái của đơn.</param>
        /// <returns>Tin nhắn hoàn chỉnh, hoặc HasTemplate=false nếu chưa có mẫu.</returns>
        public virtual async Task<OrderMessagePreviewDto> GetPreviewAsync(Guid orderId, string status)
        {
            var order = await _orders.GetAsync(orderId);

            var normalized = string.IsNullOrWhiteSpace(status) ? order.Status.ToString() : status.Trim();
            // Fail-soft: nếu client gửi trạng thái lạ, render theo trạng thái
            // thật của đơn thay vì báo lỗi — nhân viên vẫn copy được tin nhắn.
            if (!Enum.TryParse<OrderStatus>(normalized, ignoreCase: true, out var parsed))
            {
                parsed = order.Status;
                normalized = parsed.ToString();
            }

            var queryable = await _templates.GetQueryableAsync();

            // [codequality-practices 2026-10-19] Truyền predicate thẳng vào
            // FirstOrDefaultAsync thay vì bọc queryable.Where(...) thừa.
            var template = await _asyncExecuter.FirstOrDefaultAsync(
                queryable, x => x.Status == normalized && x.IsActive);

            if (template == null)
            {
                return new OrderMessagePreviewDto
                {
                    Status = normalized,
                    StatusLabel = StatusLabel(normalized),
                    Message = null,
                    HasTemplate = false
                };
            }

            return new OrderMessagePreviewDto
            {
                Status = normalized,
                StatusLabel = StatusLabel(normalized),
                Message = Render(template.Template, order),
                HasTemplate = true
            };
        }

        /// <summary>
        /// Render nội dung mẫu: thay các placeholder {MaDon}, {TenKhach},
        /// {TongTien}, {NgayDat} bằng dữ liệu đơn. Placeholder lạ giữ nguyên
        /// để Admin dễ phát hiện lỗi chính tả trong mẫu.
        /// </summary>
        /// <param name="template">Nội dung mẫu chứa placeholder.</param>
        /// <param name="order">Đơn hàng cung cấp dữ liệu.</param>
        /// <returns>Tin nhắn hoàn chỉnh; null/trống nếu không có mẫu hoặc đơn.</returns>
        public static string Render(string template, Order order)
        {
            if (string.IsNullOrEmpty(template) || order == null)
            {
                return template;
            }

            var vnTime = VietnamTime.ToVietnamTime(DateTime.UtcNow);
            return template
                .Replace("{MaDon}", order.OrderNumber ?? string.Empty)
                .Replace("{TenKhach}", order.CustomerName ?? string.Empty)
                .Replace("{TongTien}", $"{order.TotalAmount.Amount:N0} ₫")
                .Replace("{NgayDat}", vnTime.ToString("dd/MM/yyyy HH:mm"));
        }

        private static OrderMessageTemplateDto MapToDto(OrderMessageTemplate x)
        {
            return new OrderMessageTemplateDto
            {
                Id = x.Id,
                Status = x.Status,
                StatusLabel = StatusLabel(x.Status),
                Template = x.Template,
                IsActive = x.IsActive
            };
        }
    }
}
