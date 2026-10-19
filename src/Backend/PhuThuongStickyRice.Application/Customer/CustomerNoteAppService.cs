
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Customer;

namespace PhuThuongStickyRice.Application.Customer
{
    /// <summary>
    /// Ghi chú CRM dạng văn bản tự do theo từng SĐT khách hàng (chỉ admin).
    /// Nhân viên ghi lại những điểm đặc biệt về khách (dị ứng, địa chỉ giao khó
    /// tìm, thói quen đặt món...) để lần sau phục vụ chính xác hơn.
    /// </summary>
    public interface ICustomerNoteAppService : IApplicationService
    {
        /// <summary>
        /// Lấy lịch sử ghi chú của một SĐT, mới nhất trước, tối đa 200 ghi chú.
        /// </summary>
        /// <param name="phone">Số điện thoại khách hàng.</param>
        /// <returns>Danh sách ghi chú của SĐT.</returns>
        Task<List<CustomerNoteDto>> GetByPhoneAsync(string phone);

        /// <summary>
        /// Nhân viên thêm ghi chú mới cho khách; người tạo được lưu theo tài
        /// khoản đang đăng nhập để truy vết ai đã ghi chú.
        /// </summary>
        /// <param name="input">SĐT khách và nội dung ghi chú.</param>
        /// <returns>Ghi chú vừa tạo.</returns>
        Task<CustomerNoteDto> CreateAsync(CreateCustomerNoteDto input);

        /// <summary>Xóa một ghi chú (ví dụ ghi chú nhầm).</summary>
        /// <param name="id">Id ghi chú cần xóa.</param>
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// Lưu trữ ghi chú CRM của nhân viên theo SĐT khách hàng. Người tạo mỗi
    /// ghi chú lấy từ tài khoản đang đăng nhập để truy vết sau này.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class CustomerNoteAppService : ApplicationService, ICustomerNoteAppService
    {
        private readonly IRepository<CustomerNote, Guid> _notes;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>Giới hạn server-side cho lịch sử ghi chú 1 SĐT.</summary>
        private const int MaxNotesPerPhone = 200;

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
            // perf-hunt 2026-10-19 (run 14:46): Take(200) server-side — bảng
            // CustomerNote tăng vô hạn theo ghi chú của nhân viên; trước đây
            // nạp toàn bộ lịch sử ghi chú của 1 SĐT (cùng pattern đã fix ở
            // AddressBook/GetReminders run 14:35).
            var items = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.Phone == normalized)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(MaxNotesPerPhone));
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
