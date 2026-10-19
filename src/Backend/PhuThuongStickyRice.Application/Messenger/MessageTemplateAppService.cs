
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Messenger;

namespace PhuThuongStickyRice.Application.Messenger
{
    public interface IMessageTemplateAppService : IApplicationService
    {
        /// <summary>Admin: danh sách mẫu tin theo kênh (SMS/Zalo), null = tất cả.</summary>
        Task<List<MessageTemplateDto>> GetListAsync(string channel);

        /// <summary>Admin: tạo mẫu tin mới với {placeholder} trong nội dung.</summary>
        Task<MessageTemplateDto> CreateAsync(CreateUpdateMessageTemplateDto input);

        /// <summary>Admin: cập nhật mẫu tin theo Id.</summary>
        Task<MessageTemplateDto> UpdateAsync(Guid id, CreateUpdateMessageTemplateDto input);

        /// <summary>Admin: xóa mẫu tin theo Id.</summary>
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// Item 131: mẫu tin nhắn SMS/Zalo có {placeholder} để hệ thống ghép
    /// dữ liệu đơn/khách lúc gửi. Chỉ Admin quản trị.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class MessageTemplateAppService : ApplicationService, IMessageTemplateAppService
    {
        private readonly IRepository<MessageTemplate, Guid> _templates;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>Khởi tạo service quản trị mẫu tin nhắn.</summary>
        public MessageTemplateAppService(
            IRepository<MessageTemplate, Guid> templates,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _templates = templates;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Admin: danh sách mẫu tin nhắn (SMS/Zalo) theo kênh; <paramref name="channel"/>
        /// rỗng/null = lấy tất cả. Sắp xếp theo tên, tối đa 500 mẫu.
        /// </summary>
        /// <param name="channel">Kênh lọc: "SMS", "Zalo", hoặc rỗng để lấy tất cả.</param>
        /// <returns>Danh sách mẫu tin nhắn.</returns>
        public virtual async Task<List<MessageTemplateDto>> GetListAsync(string channel)
        {
            var queryable = await _templates.GetQueryableAsync();
            IQueryable<MessageTemplate> query = queryable.OrderBy(x => x.Name);
            if (!string.IsNullOrWhiteSpace(channel))
            {
                var trimmedChannel = channel.Trim();
                query = query.Where(x => x.Channel == trimmedChannel);
            }

            // perf-hunt 2026-10-19: Take(500) server-side — template do admin
            // tạo, bảng tăng không giới hạn, không bao giờ materialize toàn bộ.
            var list = await _asyncExecuter.ToListAsync(query.Take(500));
            return list.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: tạo mẫu tin nhắn mới. Nội dung hỗ trợ {placeholder} để hệ thống
        /// ghép dữ liệu đơn/khách khi gửi SMS/Zalo thực tế.
        /// </summary>
        /// <param name="input">Tên, kênh (SMS/Zalo), nội dung mẫu tin.</param>
        /// <returns>Mẫu tin nhắn vừa tạo.</returns>
        public virtual async Task<MessageTemplateDto> CreateAsync(CreateUpdateMessageTemplateDto input)
        {
            var channel = NormalizeChannel(input.Channel);
            var entity = new MessageTemplate
            {
                Name = input.Name.Trim(),
                Channel = channel,
                Body = input.Body,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _templates.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin: cập nhật tên/kênh/nội dung mẫu tin nhắn; ghi nhận thời gian sửa.
        /// </summary>
        /// <param name="id">Id mẫu tin nhắn cần sửa.</param>
        /// <param name="input">Thông tin mới của mẫu tin.</param>
        /// <returns>Mẫu tin nhắn sau khi cập nhật.</returns>
        public virtual async Task<MessageTemplateDto> UpdateAsync(Guid id, CreateUpdateMessageTemplateDto input)
        {
            var entity = await _templates.GetAsync(id);
            entity.Name = input.Name.Trim();
            entity.Channel = NormalizeChannel(input.Channel);
            entity.Body = input.Body;
            entity.UpdatedAt = DateTime.UtcNow;
            await _templates.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin: xóa mẫu tin theo Id. Xóa hẳn khỏi DB (không soft-delete vì
        /// mẫu tin không liên quan lịch sử đơn hàng).
        /// </summary>
        public virtual async Task DeleteAsync(Guid id)
        {
            await _templates.DeleteAsync(id, autoSave: true);
        }

        private static string NormalizeChannel(string channel)
        {
            var trimmed = (channel ?? string.Empty).Trim();
            if (!trimmed.Equals("SMS", StringComparison.OrdinalIgnoreCase)
                && !trimmed.Equals("Zalo", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException("Kênh phải là SMS hoặc Zalo.");
            }

            return trimmed.Equals("Zalo", StringComparison.OrdinalIgnoreCase) ? "Zalo" : "SMS";
        }

        private static MessageTemplateDto MapToDto(MessageTemplate x)
        {
            return new MessageTemplateDto
            {
                Id = x.Id,
                Name = x.Name,
                Channel = x.Channel,
                Body = x.Body
            };
        }
    }
}
