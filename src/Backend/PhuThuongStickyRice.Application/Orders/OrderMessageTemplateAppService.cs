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
    public class OrderMessageTemplateDto
    {
        public Guid Id { get; set; }

        public string Status { get; set; }

        public string StatusLabel { get; set; }

        public string Template { get; set; }

        public bool IsActive { get; set; }
    }

    public class CreateUpdateOrderMessageTemplateDto
    {
        public string Template { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class OrderMessagePreviewDto
    {
        public string Status { get; set; }

        public string StatusLabel { get; set; }

        public string Message { get; set; }

        public bool HasTemplate { get; set; }
    }

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
        private readonly IRepository<OrderMessageTemplate, Guid> _templates;
        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public OrderMessageTemplateAppService(
            IRepository<OrderMessageTemplate, Guid> templates,
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _templates = templates;
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

        public static string StatusLabel(string status) => status switch
        {
            "Confirmed" => "Đã xác nhận",
            "Preparing" => "Đang chuẩn bị",
            "Completed" => "Hoàn thành",
            "Cancelled" => "Đã hủy",
            _ => status
        };

        [Authorize(Roles = "Admin")]
        public async Task<List<OrderMessageTemplateDto>> GetListAsync()
        {
            var queryable = await _templates.GetQueryableAsync();
            var list = await _asyncExecuter.ToListAsync(queryable.OrderBy(x => x.Status));
            return list.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public async Task<OrderMessageTemplateDto> UpdateAsync(Guid id, CreateUpdateOrderMessageTemplateDto dto)
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
        public async Task<OrderMessagePreviewDto> GetPreviewAsync(Guid orderId, string status)
        {
            var order = await _orders.GetAsync(orderId);

            var normalized = string.IsNullOrWhiteSpace(status) ? order.Status.ToString() : status.Trim();
            if (!Enum.TryParse<OrderStatus>(normalized, ignoreCase: true, out var parsed))
            {
                parsed = order.Status;
                normalized = parsed.ToString();
            }

            var queryable = await _templates.GetQueryableAsync();
            var template = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Status == normalized && x.IsActive));

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
        /// Replaces {MaDon}, {TenKhach}, {TongTien}, {NgayDat}. Unknown
        /// placeholders are left untouched.
        /// </summary>
        public static string Render(string template, Order order)
        {
            if (string.IsNullOrEmpty(template) || order == null)
            {
                return template;
            }

            var vnTime = DateTime.UtcNow.AddHours(7);
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
