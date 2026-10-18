
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
    public interface IWorkShiftAppService : IApplicationService
    {
        /// <summary>Admin: shifts for the week containing the given date (Mon-Sun).</summary>
        Task<List<WorkShiftDto>> GetWeekAsync(DateTime dateInWeek);

        Task<WorkShiftDto> CreateAsync(CreateUpdateWorkShiftDto input);
        Task<WorkShiftDto> UpdateAsync(Guid id, CreateUpdateWorkShiftDto input);
        Task DeleteAsync(Guid id);
    }

    /// <summary>Item 125: staff work shifts.</summary>
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
            // Monday-first week.
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
