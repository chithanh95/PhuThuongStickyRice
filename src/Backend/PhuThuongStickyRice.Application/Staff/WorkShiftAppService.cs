
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
using PhuThuongStickyRice.Application.Staff;

namespace PhuThuongStickyRice.Application.Staff
{
    /// <summary>
    /// Quản lý lịch làm việc theo tuần của nhân viên (item 125): admin xem
    /// lịch tuần, thêm/sửa/xóa ca cho từng nhân viên từng ngày.
    /// </summary>
    public interface IWorkShiftAppService : IApplicationService
    {
        /// <summary>
        /// Admin: lấy tất cả ca trong tuần (thứ 2 → chủ nhật) chứa ngày đã
        /// cho, sắp theo ngày rồi theo giờ bắt đầu.
        /// </summary>
        /// <param name="dateInWeek">Một ngày bất kỳ trong tuần cần xem.</param>
        /// <returns>Danh sách ca của tuần đó.</returns>
        Task<List<WorkShiftDto>> GetWeekAsync(DateTime dateInWeek);

        /// <summary>
        /// Admin: thêm ca mới. Tên nhân viên bắt buộc, giờ kết thúc phải sau
        /// giờ bắt đầu; ngày lưu về dạng ngày (bỏ phần giờ).
        /// </summary>
        /// <param name="input">Nhân viên, ngày, giờ bắt đầu/kết thúc, ghi chú.</param>
        /// <returns>Ca vừa tạo.</returns>
        Task<WorkShiftDto> CreateAsync(CreateUpdateWorkShiftDto input);

        /// <summary>
        /// Admin: cập nhật ca đã xếp. Kiểm tra hợp lệ giống khi tạo.
        /// </summary>
        /// <param name="id">Id ca cần sửa.</param>
        /// <param name="input">Thông tin ca mới.</param>
        /// <returns>Ca sau khi cập nhật.</returns>
        Task<WorkShiftDto> UpdateAsync(Guid id, CreateUpdateWorkShiftDto input);

        /// <summary>
        /// Admin: xóa ca khỏi lịch.
        /// </summary>
        /// <param name="id">Id ca cần xóa.</param>
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// Lưu lịch ca làm việc của nhân viên theo tuần; tuần tính từ thứ 2 đến
    /// chủ nhật theo cách hiển thị lịch làm việc của quán. Chỉ admin quản lý.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class WorkShiftAppService : ApplicationService, IWorkShiftAppService
    {
        private readonly IRepository<WorkShift, Guid> _shifts;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public WorkShiftAppService(
            IRepository<WorkShift, Guid> shifts,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _shifts = shifts;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<List<WorkShiftDto>> GetWeekAsync(DateTime dateInWeek)
        {
            var date = dateInWeek.Date;
            // Tuần bắt đầu thứ 2: .NET quy ước DayOfWeek.Sunday = 0 nên dùng
            // công thức (+6) % 7 để chủ nhật lùi 6 ngày, các thứ còn lại lùi
            // đúng số ngày về thứ 2. Biên trên dùng < (độc quyền) nên ca đúng
            // ngày chủ nhật vẫn được tính (sunday = thứ 2 tuần sau).
            var monday = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
            var sunday = monday.AddDays(7);

            var queryable = await _shifts.GetQueryableAsync();
            var list = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.Date >= monday && x.Date < sunday)
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.StartTime));

            return list.Select(MapToDto).ToList();
        }

        public virtual async Task<WorkShiftDto> CreateAsync(CreateUpdateWorkShiftDto input)
        {
            Validate(input);
            var entity = new WorkShift
            {
                StaffName = input.StaffName.Trim(),
                Date = input.Date.Date,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            await _shifts.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        public virtual async Task<WorkShiftDto> UpdateAsync(Guid id, CreateUpdateWorkShiftDto input)
        {
            Validate(input);
            var entity = await _shifts.GetAsync(id);
            entity.StaffName = input.StaffName.Trim();
            entity.Date = input.Date.Date;
            entity.StartTime = input.StartTime;
            entity.EndTime = input.EndTime;
            entity.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
            await _shifts.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _shifts.DeleteAsync(id, autoSave: true);
        }

        /// <summary>
        /// Kiểm tra dữ liệu ca trước khi lưu: tên nhân viên bắt buộc và giờ
        /// kết thúc phải sau giờ bắt đầu (ca qua đêm chưa hỗ trợ nên coi là
        /// nhập sai giờ).
        /// </summary>
        private static void Validate(CreateUpdateWorkShiftDto input)
        {
            if (string.IsNullOrWhiteSpace(input?.StaffName))
            {
                throw new DomainException("Tên nhân viên là bắt buộc.");
            }

            if (input.EndTime <= input.StartTime)
            {
                throw new DomainException("Giờ kết thúc phải sau giờ bắt đầu.");
            }
        }

        private static WorkShiftDto MapToDto(WorkShift x)
        {
            return new WorkShiftDto
            {
                Id = x.Id,
                StaffName = x.StaffName,
                Date = x.Date,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Note = x.Note
            };
        }
    }
}
