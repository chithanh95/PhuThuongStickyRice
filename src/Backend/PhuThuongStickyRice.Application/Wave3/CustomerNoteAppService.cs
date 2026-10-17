using PhuThuongStickyRice.Application.Wave3.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Wave3
{
    /// <summary>Item 89: free-text CRM notes per customer phone (Admin).</summary>
    public interface ICustomerNoteAppService : IApplicationService
    {
        Task<List<CustomerNoteDto>> GetByPhoneAsync(string phone);

        Task<CustomerNoteDto> CreateAsync(CreateCustomerNoteDto input);

        Task DeleteAsync(Guid id);
    }

    [Authorize(Roles = "Admin")]
    public class CustomerNoteAppService : ApplicationService, ICustomerNoteAppService
    {
        private readonly IRepository<CustomerNote, Guid> _notes;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public CustomerNoteAppService(
            IRepository<CustomerNote, Guid> notes,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _notes = notes;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<List<CustomerNoteDto>> GetByPhoneAsync(string phone)
        {
            var normalized = (phone ?? string.Empty).Trim();
            var queryable = await _notes.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.Phone == normalized)
                    .OrderByDescending(x => x.CreatedAt));
            return items.Select(Map).ToList();
        }

        public virtual async Task<CustomerNoteDto> CreateAsync(CreateCustomerNoteDto input)
        {
            var note = new CustomerNote
            {
                Phone = input.Phone.Trim(),
                Note = input.Note.Trim(),
                CreatedBy = CurrentUser?.UserName,
                CreatedAt = DateTime.UtcNow
            };
            await _notes.InsertAsync(note, autoSave: true);
            return Map(note);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _notes.DeleteAsync(id, autoSave: true);
        }

        private static CustomerNoteDto Map(CustomerNote n) => new CustomerNoteDto
        {
            Id = n.Id,
            Phone = n.Phone,
            Note = n.Note,
            CreatedBy = n.CreatedBy,
            CreatedAt = n.CreatedAt
        };
    }
}
