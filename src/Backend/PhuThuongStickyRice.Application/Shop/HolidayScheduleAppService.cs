using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Shop.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Shop
{
    // [codequality-docs 2026-10-19]

    /// <summary>Hợp đồng dịch vụ quản lý lịch nghỉ lễ của quán.</summary>
    public interface IHolidayScheduleAppService : IApplicationService
    {
        /// <summary>Admin: lists all holidays, nearest first.</summary>
        Task<List<HolidayScheduleDto>> GetListAsync();

        /// <summary>Admin: adds a holiday.</summary>
        Task<HolidayScheduleDto> CreateAsync(CreateHolidayScheduleDto dto);

        /// <summary>Admin: edits a holiday.</summary>
        Task<HolidayScheduleDto> UpdateAsync(Guid id, UpdateHolidayScheduleDto dto);

        /// <summary>Admin: removes a holiday.</summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Public: upcoming holidays in the next 30 days (Vietnam date).
        /// </summary>
        Task<List<HolidayScheduleDto>> GetUpcomingAsync();
    }

    /// <summary>
    /// Dịch vụ quản lý lịch nghỉ lễ/ngày đóng cửa của quán: bộ CRUD cho admin và
    /// danh sách ngày nghỉ sắp tới (công khai) để checkout/landing biết quán có
    /// mở cửa hôm nay hay không. Dữ liệu công khai được cache và invalidate mỗi
    /// khi admin thêm/sửa/xóa.
    /// </summary>
    public class HolidayScheduleAppService : ApplicationService, IHolidayScheduleAppService
    {
        private readonly IRepository<HolidaySchedule, Guid> _holidays;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        /// <summary>Khởi tạo dịch vụ lịch nghỉ lễ.</summary>
        /// <param name="holidays">Repository lịch nghỉ.</param>
        /// <param name="asyncExecuter">Thực thi truy vấn async.</param>
        /// <param name="catalogCache">Cache dùng chung; invalidate khi lịch nghỉ thay đổi.</param>
        public HolidayScheduleAppService(
            IRepository<HolidaySchedule, Guid> holidays,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _holidays = holidays;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        /// <summary>Vietnam calendar date (UTC+7, no DST).</summary>
        internal static DateTime VietnamToday() => VietnamTime.Today();

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<HolidayScheduleDto>> GetListAsync()
        {
            var queryable = await _holidays.GetQueryableAsync();
            var list = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.HolidayDate));
            return list.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<HolidayScheduleDto> CreateAsync(CreateHolidayScheduleDto dto)
        {
            var (date, reason) = Validate(dto.HolidayDate, dto.Reason);

            var queryable = await _holidays.GetQueryableAsync();
            var exists = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.HolidayDate == date));
            if (exists != null)
            {
                throw new Domain.Exceptions.DomainException(
                    $"Ngày {date:dd/MM/yyyy} đã có trong lịch nghỉ.");
            }

            var entity = new HolidaySchedule
            {
                HolidayDate = date,
                Reason = reason,
                IsFullDay = dto.IsFullDay,
                CreatedAt = DateTime.UtcNow
            };
            await _holidays.InsertAsync(entity, autoSave: true);
            // Perf: bust the cached per-day holiday reason used by public shop settings.
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<HolidayScheduleDto> UpdateAsync(Guid id, UpdateHolidayScheduleDto dto)
        {
            var entity = await _holidays.GetAsync(id);
            var (date, reason) = Validate(dto.HolidayDate, dto.Reason);

            var queryable = await _holidays.GetQueryableAsync();
            var clash = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.HolidayDate == date && x.Id != id));
            if (clash != null)
            {
                throw new Domain.Exceptions.DomainException(
                    $"Ngày {date:dd/MM/yyyy} đã có trong lịch nghỉ.");
            }

            entity.HolidayDate = date;
            entity.Reason = reason;
            entity.IsFullDay = dto.IsFullDay;
            await _holidays.UpdateAsync(entity, autoSave: true);
            // Perf: bust the cached per-day holiday reason used by public shop settings.
            _catalogCache.Invalidate();
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task DeleteAsync(Guid id)
        {
            await _holidays.DeleteAsync(id, autoSave: true);
            // Perf: bust the cached per-day holiday reason used by public shop settings.
            _catalogCache.Invalidate();
        }

        [AllowAnonymous]
        public virtual async Task<List<HolidayScheduleDto>> GetUpcomingAsync()
        {
            // Perf: public shop calls this on every checkout/landing load, but
            // holidays change rarely and Create/Update/Delete already call
            // _catalogCache.Invalidate(). The Vietnam date is part of the key so
            // a midnight rollover never serves a stale day's list; the 5-minute
            // catalog TTL covers the rest. No business change.
            var today = VietnamToday();
            return await _catalogCache.GetOrAddAsync(
                $"holidays:upcoming:{today:yyyyMMdd}",
                QueryUpcomingAsync);
        }

        private async Task<List<HolidayScheduleDto>> QueryUpcomingAsync()
        {
            var today = VietnamToday();
            var limit = today.AddDays(30);
            var queryable = await _holidays.GetQueryableAsync();
            var list = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.HolidayDate >= today && x.HolidayDate <= limit)
                    .OrderBy(x => x.HolidayDate));
            return list.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Kiểm tra và chuẩn hóa ngày nghỉ trước khi lưu: loại bỏ phần giờ của ngày
        /// đã nhập, từ chối ngày quá khứ (so theo lịch Việt Nam vì quán bán theo giờ
        /// VN — một "ngày hôm qua" theo UTC vẫn có thể là hôm nay ở VN), bắt buộc
        /// nhập lý do, và cắt lý do quá dài về giới hạn của entity thay vì lỗi.
        /// </summary>
        /// <param name="holidayDate">Ngày nghỉ do admin nhập (phần giờ bị bỏ).</param>
        /// <param name="reason">Lý do nghỉ; bắt buộc có.</param>
        /// <returns>Cặp (ngày đã chuẩn hóa, lý do đã cắt gọn).</returns>
        private static (DateTime date, string reason) Validate(DateTime holidayDate, string reason)
        {
            var date = holidayDate.Date;
            if (date < VietnamToday().AddDays(-1))
            {
                throw new Domain.Exceptions.DomainException("Không thể thêm ngày nghỉ trong quá khứ.");
            }

            reason = (reason ?? string.Empty).Trim();
            if (reason.Length == 0)
            {
                throw new Domain.Exceptions.DomainException("Vui lòng nhập lý do nghỉ (VD: Nghỉ Tết Nguyên đán).");
            }

            if (reason.Length > HolidaySchedule.ReasonMaxLength)
            {
                reason = reason.Substring(0, HolidaySchedule.ReasonMaxLength);
            }

            return (date, reason);
        }

        private static HolidayScheduleDto MapToDto(HolidaySchedule x)
        {
            return new HolidayScheduleDto
            {
                Id = x.Id,
                HolidayDate = x.HolidayDate,
                Reason = x.Reason,
                IsFullDay = x.IsFullDay
            };
        }
    }
}
