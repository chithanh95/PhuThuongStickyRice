using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.WeeklyPlans
{
    public interface IWeeklyPlanAppService : IApplicationService
    {
        /// <summary>Public: gói tuần đang bán (thứ 2 gần nhất &lt;= hôm nay).</summary>
        Task<WeeklyPlanDto> GetActiveAsync();

        /// <summary>Admin: tạo gói tuần mới.</summary>
        Task<WeeklyPlanDto> CreateAsync(CreateWeeklyPlanDto dto);

        /// <summary>Admin: danh sách gói (mới nhất trước).</summary>
        Task<List<WeeklyPlanDto>> GetListAsync();

        /// <summary>Public: khách mua gói.</summary>
        Task<WeeklyPlanOrderDto> BuyAsync(BuyWeeklyPlanDto dto);

        /// <summary>Public: đơn mua gói theo SĐT.</summary>
        Task<List<WeeklyPlanOrderDto>> GetMyOrdersAsync(string phone);

        /// <summary>Public: khách tự bấm "Đã nhận" cho 1 ngày.</summary>
        Task<WeeklyPlanOrderDto> MarkClaimedAsync(Guid orderId, string day);
    }

    /// <summary>
    /// Gói "Ăn sáng cả tuần" (item 185): gói T2–T6 do quán xếp sẵn,
    /// giảm 10% so với mua lẻ từng món.
    /// </summary>
    public class WeeklyPlanAppService : ApplicationService, IWeeklyPlanAppService
    {
        private static readonly string[] ValidDays = { "Mon", "Tue", "Wed", "Thu", "Fri" };

        private const string PhonePattern = @"^(0)(3|5|7|8|9)\d{8}$";

        private readonly IRepository<WeeklyPlan, Guid> _plans;
        private readonly IRepository<WeeklyPlanOrder, Guid> _orders;
        private readonly IRepository<Product, Guid> _products;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>perf-hunt 2026-10-19: cache for the public active-plan lookup (shop tab hot path).</summary>
        private readonly Products.IProductCatalogCache _catalogCache;

        public WeeklyPlanAppService(
            IRepository<WeeklyPlan, Guid> plans,
            IRepository<WeeklyPlanOrder, Guid> orders,
            IRepository<Product, Guid> products,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _plans = plans;
            _orders = orders;
            _products = products;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        [AllowAnonymous]
        public virtual async Task<WeeklyPlanDto> GetActiveAsync()
        {
            // Perf: shop tab hits this on every view; cache 5 minutes.
            // Keyed by the Monday date so the plan rolls over naturally each week.
            var monday = GetMonday(DateTime.Today);
            return await _catalogCache.GetOrAddAsync(
                $"weeklyplan:active:{monday:yyyyMMdd}", async () =>
                {
                    var queryable = await _plans.GetQueryableAsync();
                    var plan = await _asyncExecuter.FirstOrDefaultAsync(
                        queryable
                            .Where(p => p.IsActive && p.WeekStart <= monday)
                            .OrderByDescending(p => p.WeekStart));

                    return plan == null ? null : MapToDto(plan);
                });
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<WeeklyPlanDto> CreateAsync(CreateWeeklyPlanDto dto)
        {
            if (dto == null || dto.Days == null || dto.Days.Count != 5)
            {
                throw new DomainException("Gói tuần cần đủ 5 ngày từ thứ 2 đến thứ 6.");
            }

            var orderedDays = ValidDays.ToList();
            var days = dto.Days.ToList();
            foreach (var d in days)
            {
                if (string.IsNullOrWhiteSpace(d.DayOfWeek) ||
                    !orderedDays.Contains(d.DayOfWeek.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    throw new DomainException("Ngày trong gói phải là Mon..Fri.");
                }

                if (d.ProductId == Guid.Empty)
                {
                    throw new DomainException("Chưa chọn món cho đầy đủ các ngày.");
                }
            }

            var distinct = days.Select(d => d.DayOfWeek.Trim().ToLowerInvariant()).Distinct().Count();
            if (distinct != 5)
            {
                throw new DomainException("Mỗi ngày từ thứ 2 đến thứ 6 chỉ được chọn 1 lần.");
            }

            // Snap về ngày thứ 2 của tuần đó.
            var weekStart = GetMonday(dto.WeekStart.Date);

            var dayDtos = new List<WeeklyPlanDayDto>();
            // Perf: 1 query duy nhất thay vì 5 lần GetAsync trong loop (N+1).
            var productIds = days.Select(d => d.ProductId).Distinct().ToList();
            var productQueryable = await _products.GetQueryableAsync();
            var productsById = (await _asyncExecuter.ToListAsync(
                    productQueryable.Where(p => productIds.Contains(p.Id))))
                .ToDictionary(p => p.Id);

            foreach (var dayCode in orderedDays)
            {
                var input = days.First(d =>
                    d.DayOfWeek.Trim().Equals(dayCode, StringComparison.OrdinalIgnoreCase));
                if (!productsById.TryGetValue(input.ProductId, out var product))
                {
                    throw new DomainException($"Không tìm thấy sản phẩm cho {dayCode}.");
                }

                dayDtos.Add(new WeeklyPlanDayDto
                {
                    DayOfWeek = dayCode,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price?.Amount ?? 0,
                });
            }

            var total = dayDtos.Sum(d => d.Price);
            var plan = new WeeklyPlan
            {
                WeekStart = weekStart,
                DayMenusJson = JsonSerializer.Serialize(dayDtos),
                Price = Math.Round(total * 0.9m, 0),
                IsActive = true,
            };
            await _plans.InsertAsync(plan);

            // Gói mới thay thế gói cũ cùng tuần.
            var queryable = await _plans.GetQueryableAsync();
            var oldPlans = await _asyncExecuter.ToListAsync(
                queryable.Where(p => p.Id != plan.Id && p.WeekStart == weekStart && p.IsActive));
            foreach (var old in oldPlans)
            {
                old.IsActive = false;
                await _plans.UpdateAsync(old);
            }

            // Perf: bust the cached public active plan.
            _catalogCache.Invalidate();

            return MapToDto(plan);
        }

        [Authorize(Roles = "Admin")]
        public virtual async Task<List<WeeklyPlanDto>> GetListAsync()
        {
            var planQueryable = await _plans.GetQueryableAsync();
            var plans = await _asyncExecuter.ToListAsync(
                planQueryable.OrderByDescending(p => p.WeekStart));

            var orderQueryable = await _orders.GetQueryableAsync();
            // Perf: GroupBy chạy trong SQL thay vì nạp toàn bộ bảng vào memory.
            var counts = (await _asyncExecuter.ToListAsync(
                    orderQueryable.GroupBy(o => o.WeeklyPlanId)
                        .Select(g => new { WeeklyPlanId = g.Key, Count = g.Count() })))
                .ToDictionary(x => x.WeeklyPlanId, x => x.Count);

            return plans.Select(p =>
            {
                var dto = MapToDto(p);
                dto.BuyerCount = counts.TryGetValue(p.Id, out var c) ? c : 0;
                return dto;
            }).ToList();
        }

        [AllowAnonymous]
        public virtual async Task<WeeklyPlanOrderDto> BuyAsync(BuyWeeklyPlanDto dto)
        {
            if (dto == null)
            {
                throw new DomainException("Thiếu thông tin mua gói.");
            }

            if (string.IsNullOrWhiteSpace(dto.CustomerName))
            {
                throw new DomainException("Vui lòng nhập tên của bạn.");
            }

            var phone = (dto.CustomerPhone ?? string.Empty).Trim();
            EnsureValidPhone(phone);

            var plan = await _plans.GetAsync(dto.WeeklyPlanId);
            if (plan == null || !plan.IsActive)
            {
                throw new DomainException("Gói tuần này không còn bán nữa.");
            }

            var order = new WeeklyPlanOrder
            {
                WeeklyPlanId = plan.Id,
                CustomerName = dto.CustomerName.Trim(),
                CustomerPhone = phone,
                PaidAmount = plan.Price,
                OrderCode = await GenerateOrderCodeAsync(),
                ClaimedDaysJson = JsonSerializer.Serialize(
                    ValidDays.ToDictionary(d => d, _ => false)),
                Status = WeeklyPlanOrderStatus.Paid,
            };
            await _orders.InsertAsync(order);

            return MapToDto(order);
        }

        [AllowAnonymous]
        public virtual async Task<List<WeeklyPlanOrderDto>> GetMyOrdersAsync(string phone)
        {
            phone = (phone ?? string.Empty).Trim();
            EnsureValidPhone(phone);

            var queryable = await _orders.GetQueryableAsync();
            var orders = await _asyncExecuter.ToListAsync(
                queryable
                    .Where(o => o.CustomerPhone == phone)
                    .OrderByDescending(o => o.CreatedAt));

            return orders.Select(MapToDto).ToList();
        }

        [AllowAnonymous]
        public virtual async Task<WeeklyPlanOrderDto> MarkClaimedAsync(Guid orderId, string day)
        {
            day = (day ?? string.Empty).Trim();
            if (!ValidDays.Contains(day, StringComparer.OrdinalIgnoreCase))
            {
                throw new DomainException("Ngày không hợp lệ (chỉ Mon..Fri).");
            }

            var order = await _orders.GetAsync(orderId);
            if (order.Status != WeeklyPlanOrderStatus.Paid)
            {
                throw new DomainException("Đơn này đã hoàn tất hoặc đã hủy.");
            }

            var claimed = ParseClaimedDays(order.ClaimedDaysJson);
            var key = ValidDays.First(d => d.Equals(day, StringComparison.OrdinalIgnoreCase));
            claimed[key] = true;
            order.ClaimedDaysJson = JsonSerializer.Serialize(claimed);

            if (ValidDays.All(d => claimed.TryGetValue(d, out var v) && v))
            {
                order.Status = WeeklyPlanOrderStatus.Completed;
            }

            await _orders.UpdateAsync(order);
            return MapToDto(order);
        }

        // ---- helpers ----

        private static DateTime GetMonday(DateTime date)
        {
            // DayOfWeek: Sunday=0..Saturday=6 → Monday=0 offset.
            var offset = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-offset);
        }

        private static void EnsureValidPhone(string phone)
        {
            if (!Regex.IsMatch(phone ?? string.Empty, PhonePattern))
            {
                throw new DomainException("Số điện thoại không hợp lệ (VD: 0912345678).");
            }
        }

        private async Task<string> GenerateOrderCodeAsync()
        {
            var queryable = await _orders.GetQueryableAsync();
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var code = "TUAN" + Random.Shared.Next(100000, 999999).ToString();
                var exists = await _asyncExecuter.AnyAsync(
                    queryable.Where(o => o.OrderCode == code));
                if (!exists)
                {
                    return code;
                }
            }

            throw new DomainException("Không sinh được mã đơn, vui lòng thử lại.");
        }

        private static WeeklyPlanDto MapToDto(WeeklyPlan plan)
        {
            List<WeeklyPlanDayDto> days;
            try
            {
                days = JsonSerializer.Deserialize<List<WeeklyPlanDayDto>>(plan.DayMenusJson)
                    ?? new List<WeeklyPlanDayDto>();
            }
            catch
            {
                days = new List<WeeklyPlanDayDto>();
            }

            return new WeeklyPlanDto
            {
                Id = plan.Id,
                WeekStart = plan.WeekStart,
                Days = days,
                Price = plan.Price,
                IsActive = plan.IsActive,
            };
        }

        private static WeeklyPlanOrderDto MapToDto(WeeklyPlanOrder order)
        {
            Dictionary<string, bool> claimed = ParseClaimedDays(order.ClaimedDaysJson);

            return new WeeklyPlanOrderDto
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                PaidAmount = order.PaidAmount,
                ClaimedDays = claimed,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
            };
        }

        private static Dictionary<string, bool> ParseClaimedDays(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, bool>>(json)
                    ?? new Dictionary<string, bool>();
            }
            catch
            {
                return new Dictionary<string, bool>();
            }
        }
    }
}
