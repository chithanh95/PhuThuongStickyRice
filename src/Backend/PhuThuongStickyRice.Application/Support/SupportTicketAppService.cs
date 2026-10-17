using PhuThuongStickyRice.Application.Support.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Support
{
    public interface ISupportTicketAppService : IApplicationService
    {
        /// <summary>Public: customer opens a support ticket.</summary>
        Task<SupportTicketDto> CreateAsync(CreateSupportTicketDto dto);

        /// <summary>Admin: ticket list, newest first.</summary>
        Task<List<SupportTicketDto>> GetListAsync(string status);

        /// <summary>Admin: reply + set status.</summary>
        Task<SupportTicketDto> ReplyAsync(Guid id, ReplySupportTicketDto dto);
    }

    public class SupportTicketAppService : ApplicationService, ISupportTicketAppService
    {
        private readonly IRepository<SupportTicket, Guid> _tickets;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public SupportTicketAppService(
            IRepository<SupportTicket, Guid> tickets,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _tickets = tickets;
            _asyncExecuter = asyncExecuter;
        }

        [AllowAnonymous]
        public virtual async Task<SupportTicketDto> CreateAsync(CreateSupportTicketDto dto)
        {
            var ticket = new SupportTicket
            {
                Name = dto.Name.Trim(),
                Phone = dto.Phone.Trim(),
                OrderNumber = string.IsNullOrWhiteSpace(dto.OrderNumber) ? null : dto.OrderNumber.Trim(),
                Message = dto.Message.Trim(),
                Status = SupportTicketStatus.New,
                CreatedAt = DateTime.UtcNow
            };
            await _tickets.InsertAsync(ticket, autoSave: true);
            return MapToDto(ticket);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<SupportTicketDto>> GetListAsync(string status)
        {
            var queryable = await _tickets.GetQueryableAsync();
            IQueryable<SupportTicket> query = queryable.OrderByDescending(x => x.CreatedAt);

            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<SupportTicketStatus>(status, ignoreCase: true, out var parsed))
            {
                query = query.Where(x => x.Status == parsed);
            }

            var items = await _asyncExecuter.ToListAsync(query.Take(200));
            return items.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<SupportTicketDto> ReplyAsync(Guid id, ReplySupportTicketDto dto)
        {
            var ticket = await _tickets.GetAsync(id);

            if (!Enum.TryParse<SupportTicketStatus>(dto.Status, ignoreCase: true, out var status)
                || status == SupportTicketStatus.New)
            {
                throw new Domain.Exceptions.DomainException("Trạng thái không hợp lệ.");
            }

            ticket.ReplyNote = dto.ReplyNote.Trim();
            ticket.Status = status;
            ticket.RepliedAt = DateTime.UtcNow;
            await _tickets.UpdateAsync(ticket, autoSave: true);
            return MapToDto(ticket);
        }

        private static SupportTicketDto MapToDto(SupportTicket ticket)
        {
            return new SupportTicketDto
            {
                Id = ticket.Id,
                Name = ticket.Name,
                Phone = ticket.Phone,
                OrderNumber = ticket.OrderNumber,
                Message = ticket.Message,
                Status = ticket.Status.ToString(),
                ReplyNote = ticket.ReplyNote,
                CreatedAt = ticket.CreatedAt,
                RepliedAt = ticket.RepliedAt
            };
        }
    }
}
