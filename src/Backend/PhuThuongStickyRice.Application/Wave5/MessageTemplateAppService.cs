using PhuThuongStickyRice.Application.Wave5.DTOs;
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

namespace PhuThuongStickyRice.Application.Wave5
{
    public interface IMessageTemplateAppService : IApplicationService
    {
        Task<List<MessageTemplateDto>> GetListAsync(string channel);
        Task<MessageTemplateDto> CreateAsync(CreateUpdateMessageTemplateDto input);
        Task<MessageTemplateDto> UpdateAsync(Guid id, CreateUpdateMessageTemplateDto input);
        Task DeleteAsync(Guid id);
    }

    /// <summary>Item 131: SMS / Zalo message templates with {placeholders}.</summary>
    [Authorize(Roles = "Admin")]
    public class MessageTemplateAppService : ApplicationService, IMessageTemplateAppService
    {
        private readonly IRepository<MessageTemplate, Guid> _templates;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public MessageTemplateAppService(
            IRepository<MessageTemplate, Guid> templates,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _templates = templates;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<List<MessageTemplateDto>> GetListAsync(string channel)
        {
            var queryable = await _templates.GetQueryableAsync();
            IQueryable<MessageTemplate> query = queryable.OrderBy(x => x.Name);
            if (!string.IsNullOrWhiteSpace(channel))
            {
                var c = channel.Trim();
                query = query.Where(x => x.Channel == c);
            }

            var list = await _asyncExecuter.ToListAsync(query);
            return list.Select(MapToDto).ToList();
        }

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

        public virtual async Task DeleteAsync(Guid id)
        {
            await _templates.DeleteAsync(id, autoSave: true);
        }

        private static string NormalizeChannel(string channel)
        {
            var c = (channel ?? string.Empty).Trim();
            if (!c.Equals("SMS", StringComparison.OrdinalIgnoreCase)
                && !c.Equals("Zalo", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException("Kênh phải là SMS hoặc Zalo.");
            }

            return c.Equals("Zalo", StringComparison.OrdinalIgnoreCase) ? "Zalo" : "SMS";
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
