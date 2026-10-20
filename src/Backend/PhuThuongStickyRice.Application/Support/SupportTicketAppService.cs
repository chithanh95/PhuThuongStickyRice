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
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Support
{
    public interface ISupportTicketAppService : IApplicationService
    {
        /// <summary>Khách hàng gửi yêu cầu hỗ trợ (cho phép ẩn danh, không cần đăng nhập).</summary>
        /// <param name="dto">Tên, SĐT, mã đơn (tùy chọn) và nội dung cần hỗ trợ.</param>
        Task<SupportTicketDto> CreateAsync(CreateSupportTicketDto dto);

        /// <summary>Admin: danh sách ticket, mới nhất trước; lọc theo trạng thái nếu truyền.</summary>
        /// <param name="status">Tên trạng thái (vd "New"); trống hoặc parse lỗi thì lấy tất cả.</param>
        Task<List<SupportTicketDto>> GetListAsync(string status);

        /// <summary>Admin: trả lời ticket và chuyển trạng thái (không được trả về "New").</summary>
        /// <param name="id">Id ticket.</param>
        /// <param name="dto">Nội dung trả lời và trạng thái mới.</param>
        Task<SupportTicketDto> ReplyAsync(Guid id, ReplySupportTicketDto dto);
    }

    /// <summary>
    /// Quản lý ticket hỗ trợ khách hàng: khách gửi yêu cầu (ẩn danh được),
    /// admin xem danh sách và trả lời. Không dùng hệ thống email/ticket
    /// ngoài — mọi thứ nằm trong DB nội bộ, SĐT là kênh liên hệ chính.
    /// </summary>
    public class SupportTicketAppService : ApplicationService, ISupportTicketAppService
    {
        private readonly IRepository<SupportTicket, Guid> _tickets;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        // [codequality-smell] Magic number: số ticket tối đa trả về 1 lần —
        // đặt hằng số có tên để thay đổi 1 chỗ.
        private const int MaxListRows = 200;

        /// <summary>Khởi tạo service quản lý ticket hỗ trợ.</summary>
        public SupportTicketAppService(
            IRepository<SupportTicket, Guid> tickets,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _tickets = tickets;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Khách hàng tạo ticket hỗ trợ mới. Cho phép ẩn danh vì nhiều khách
        /// chưa có tài khoản — SĐT là thông tin duy nhất để quán liên hệ lại.
        /// Trạng thái ban đầu luôn là New.
        /// </summary>
        /// <param name="dto">Tên, SĐT, mã đơn (tùy chọn) và nội dung cần hỗ trợ.</param>
        /// <returns>Ticket vừa tạo.</returns>
        [AllowAnonymous]
        public virtual async Task<SupportTicketDto> CreateAsync(CreateSupportTicketDto dto)
        {
            var ticket = new SupportTicket
            {
                Name = dto.Name.Trim(),
                Phone = dto.Phone.Trim(),
                OrderNumber = StringHelpers.TrimToNull(dto.OrderNumber),
                Message = dto.Message.Trim(),
                Status = SupportTicketStatus.New,
                CreatedAt = DateTime.UtcNow
            };
            await _tickets.InsertAsync(ticket, autoSave: true);
            return MapToDto(ticket);
        }

        /// <summary>
        /// Admin: danh sách ticket, mới nhất trước, tối đa 200 ticket
        /// (đủ cho màn hình xử lý; không lấy hết lịch sử).
        /// </summary>
        /// <param name="status">
        /// Lọc theo tên trạng thái (không phân biệt hoa thường). Trống hoặc
        /// parse lỗi thì bỏ lọc — tránh lỗi 500 khi client gửi giá trị lạ.
        /// </param>
        /// <returns>Danh sách ticket.</returns>
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

            var items = await _asyncExecuter.ToListAsync(query.Take(MaxListRows));
            return items.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: lưu lời nhắn trả lời và chuyển trạng thái ticket.
        /// Từ chối trạng thái không parse được hoặc trả về New — ticket đã
        /// trả lời không được quay lại "mới", giữ lịch sử xử lý đúng thứ tự.
        /// </summary>
        /// <param name="id">Id ticket.</param>
        /// <param name="dto">Nội dung trả lời và trạng thái mới.</param>
        /// <returns>Ticket sau khi trả lời.</returns>
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
