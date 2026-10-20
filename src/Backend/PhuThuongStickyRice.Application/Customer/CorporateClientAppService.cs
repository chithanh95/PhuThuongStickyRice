
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Customer
{
    /// <summary>
    /// Quản lý khách hàng doanh nghiệp đặt món định kỳ theo tuần (item 122).
    /// Mỗi công ty gắn một thứ trong tuần giao món cố định; sáng mỗi ngày
    /// hệ thống lọc ra các công ty "đến hẹn" để chuẩn bị đơn.
    /// </summary>
    public interface ICorporateClientAppService : IApplicationService
    {
        /// <summary>
        /// Admin: danh sách công ty, sắp theo tên, tối đa 200 dòng (server-side).
        /// </summary>
        /// <returns>Danh sách khách hàng doanh nghiệp.</returns>
        Task<List<CorporateClientDto>> GetListAsync();

        /// <summary>
        /// Admin: xem chi tiết một công ty.
        /// </summary>
        /// <param name="id">Id khách hàng doanh nghiệp.</param>
        /// <returns>Thông tin chi tiết công ty.</returns>
        Task<CorporateClientDto> GetAsync(Guid id);

        /// <summary>
        /// Admin: thêm công ty mới. SĐT được chuẩn hóa về dạng chữ số;
        /// tên công ty là bắt buộc, các trường còn lại có thể để trống.
        /// </summary>
        /// <param name="input">Thông tin công ty (tên, liên hệ, SĐT, thứ giao, món đặt).</param>
        /// <returns>Công ty vừa tạo.</returns>
        Task<CorporateClientDto> CreateAsync(CreateUpdateCorporateClientDto input);

        /// <summary>
        /// Admin: cập nhật thông tin công ty (kể cả bật/tắt trạng thái hoạt động).
        /// </summary>
        /// <param name="id">Id công ty.</param>
        /// <param name="input">Thông tin mới.</param>
        /// <returns>Công ty sau khi cập nhật.</returns>
        Task<CorporateClientDto> UpdateAsync(Guid id, CreateUpdateCorporateClientDto input);

        /// <summary>
        /// Admin: xóa công ty khỏi danh sách đặt định kỳ.
        /// </summary>
        /// <param name="id">Id công ty.</param>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Khách đặt theo lịch định kỳ: lấy các công ty đang hoạt động có thứ
        /// giao món trùng với thứ hôm nay (tính theo giờ Việt Nam).
        /// </summary>
        /// <returns>Danh sách công ty "đến hẹn" hôm nay.</returns>
        Task<List<CorporateClientDto>> GetDueTodayAsync();
    }

    /// <summary>
    /// Lưu trữ và tra cứu khách hàng doanh nghiệp đặt món định kỳ theo tuần.
    /// "Thứ trong tuần" của mỗi công ty được so theo ngày Việt Nam (không
    /// dùng giờ UTC của server) vì khách đặt theo lịch sinh hoạt thực tế.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class CorporateClientAppService : ApplicationService, ICorporateClientAppService
    {
        private readonly IRepository<CorporateClient, Guid> _clients;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public CorporateClientAppService(
            IRepository<CorporateClient, Guid> clients,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _clients = clients;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Admin: danh sách công ty sắp theo tên, tối đa 200 dòng (server-side) —
        /// bảng khách hàng doanh nghiệp tăng theo thời gian nên không materialize toàn bộ.
        /// </summary>
        /// <returns>Danh sách khách hàng doanh nghiệp.</returns>
        public virtual async Task<List<CorporateClientDto>> GetListAsync()
        {
            var queryable = await _clients.GetQueryableAsync();
            // perf-hunt 2026-10-19 (run 14:15): chặn số dòng ở server (Take 200)
            // như các admin list khác — bảng khách hàng doanh nghiệp tăng
            // theo thời gian, không nên materialize toàn bộ mỗi lần mở trang.
            var list = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.CompanyName).Take(200));
            return list.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: xem chi tiết một công ty.
        /// </summary>
        /// <param name="id">Id khách hàng doanh nghiệp.</param>
        /// <returns>Thông tin chi tiết công ty; ném exception khi Id không tồn tại.</returns>
        public virtual async Task<CorporateClientDto> GetAsync(Guid id)
        {
            return MapToDto(await _clients.GetAsync(id));
        }

        /// <summary>
        /// Admin: thêm công ty mới. SĐT được chuẩn hóa về dạng chữ số; nếu SĐT
        /// nhập vào chưa đủ 9 chữ số (chuẩn hóa thất bại) thì giữ nguyên chuỗi
        /// gốc để không mất dữ liệu — admin có thể sửa lại sau.
        /// </summary>
        /// <param name="input">Thông tin công ty (tên, liên hệ, SĐT, thứ giao, món đặt).</param>
        /// <returns>Công ty vừa tạo.</returns>
        public virtual async Task<CorporateClientDto> CreateAsync(CreateUpdateCorporateClientDto input)
        {
            var entity = new CorporateClient
            {
                CompanyName = input.CompanyName.Trim(),
                ContactName = string.IsNullOrWhiteSpace(input.ContactName) ? null : input.ContactName.Trim(),
                Phone = VietnamTime.NormalizePhone(input.Phone) ?? input.Phone.Trim(),
                Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim(),
                Weekday = input.Weekday,
                ItemsJson = string.IsNullOrWhiteSpace(input.ItemsJson) ? null : input.ItemsJson,
                IsActive = input.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _clients.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin: cập nhật thông tin công ty (kể cả bật/tắt trạng thái hoạt động).
        /// </summary>
        /// <param name="id">Id công ty.</param>
        /// <param name="input">Thông tin mới.</param>
        /// <returns>Công ty sau khi cập nhật.</returns>
        public virtual async Task<CorporateClientDto> UpdateAsync(Guid id, CreateUpdateCorporateClientDto input)
        {
            var entity = await _clients.GetAsync(id);
            entity.CompanyName = input.CompanyName.Trim();
            entity.ContactName = string.IsNullOrWhiteSpace(input.ContactName) ? null : input.ContactName.Trim();
            entity.Phone = VietnamTime.NormalizePhone(input.Phone) ?? input.Phone.Trim();
            entity.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
            entity.Weekday = input.Weekday;
            entity.ItemsJson = string.IsNullOrWhiteSpace(input.ItemsJson) ? null : input.ItemsJson;
            entity.IsActive = input.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _clients.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        /// <summary>
        /// Admin: xóa công ty khỏi danh sách đặt định kỳ.
        /// </summary>
        /// <param name="id">Id công ty.</param>
        public virtual async Task DeleteAsync(Guid id)
        {
            await _clients.DeleteAsync(id, autoSave: true);
        }

        /// <summary>
        /// Lấy các công ty đang hoạt động có thứ giao món trùng với thứ hôm nay
        /// (tính theo giờ Việt Nam, vì khách lên lịch theo ngày họ nhìn thấy).
        /// Hệ thống gọi mỗi sáng để biết cần chuẩn bị đơn cho những công ty nào.
        /// </summary>
        /// <returns>Danh sách công ty "đến hẹn" hôm nay, sắp theo tên.</returns>
        public virtual async Task<List<CorporateClientDto>> GetDueTodayAsync()
        {
            // Tính "hôm nay" theo giờ Việt Nam: khách lên lịch giao món theo
            // ngày dương lịch họ nhìn thấy, không theo múi giờ UTC của server.
            var weekday = (int)VietnamTime.Today().DayOfWeek;
            var queryable = await _clients.GetQueryableAsync();
            var list = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive && x.Weekday == weekday)
                    .OrderBy(x => x.CompanyName));
            return list.Select(MapToDto).ToList();
        }

        private static CorporateClientDto MapToDto(CorporateClient x)
        {
            return new CorporateClientDto
            {
                Id = x.Id,
                CompanyName = x.CompanyName,
                ContactName = x.ContactName,
                Phone = x.Phone,
                Address = x.Address,
                Weekday = x.Weekday,
                // Tên thứ hiển thị bằng tiếng Việt (Thứ hai…Chủ nhật) để admin
                // đọc lịch giao món của công ty cho dễ, thay vì DayOfWeek tiếng Anh.
                WeekdayName = VietnameseCulture.Value.DateTimeFormat.DayNames[x.Weekday],
                ItemsJson = x.ItemsJson,
                IsActive = x.IsActive
            };
        }
    }
}
