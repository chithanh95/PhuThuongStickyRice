
// CS8632: file dùng annotation nullable (?); bật ngữ cảnh annotations để
// compiler hiểu đúng ý nghĩa, không bật phân tích luồng (dự án chưa bật nullable toàn cục).
#nullable enable annotations

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
        // [codequality-practices 2026-10-20] string?: doc ghi rõ "null = tất
        // cả" — impl đã null-safe bằng IsNullOrWhiteSpace.
        Task<List<MessageTemplateDto>> GetListAsync(string? channel);

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

        /// <summary>
        /// Số mẫu tin nhắn tối đa lấy ở server cho danh sách admin — template
        /// do admin tạo, bảng tăng không giới hạn, không bao giờ materialize
        /// toàn bộ; đặt tên hằng số thay vì hardcode 500 trong truy vấn.
        /// </summary>
        private const int MaxListRows = 500;

        /// <summary>Kênh mẫu tin SMS — giá trị lưu trong DB.</summary>
        private const string ChannelSms = "SMS";

        /// <summary>Kênh mẫu tin Zalo — giá trị lưu trong DB.</summary>
        private const string ChannelZalo = "Zalo";

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
        public virtual async Task<List<MessageTemplateDto>> GetListAsync(string? channel)
        {
            var queryable = await _templates.GetQueryableAsync();
            IQueryable<MessageTemplate> query = queryable.OrderBy(x => x.Name);
            if (!string.IsNullOrWhiteSpace(channel))
            {
                var trimmedChannel = channel.Trim();
                query = query.Where(x => x.Channel == trimmedChannel);
            }

            // perf-hunt 2026-10-19: Take(MaxListRows) server-side — template do
            // admin tạo, bảng tăng không giới hạn, không bao giờ materialize
            // toàn bộ.
            var list = await _asyncExecuter.ToListAsync(query.Take(MaxListRows));
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
            if (!trimmed.Equals(ChannelSms, StringComparison.OrdinalIgnoreCase)
                && !trimmed.Equals(ChannelZalo, StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException("Kênh phải là SMS hoặc Zalo.");
            }

            return trimmed.Equals(ChannelZalo, StringComparison.OrdinalIgnoreCase) ? ChannelZalo : ChannelSms;
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
