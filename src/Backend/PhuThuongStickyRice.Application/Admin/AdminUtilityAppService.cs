using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Products;
using PhuThuongStickyRice.Application.Products.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PhuThuongStickyRice.Application.Admin
{
    public interface IAdminUtilityAppService : IApplicationService
    {
        /// <summary>Item 157: global admin search across orders, products, customers.</summary>
        Task<GlobalSearchResultDto> SearchAsync(string q);

        /// <summary>Item 158: aggregated admin notifications.</summary>
        Task<List<NotificationDto>> GetNotificationsAsync();
    }

    public class AdminUtilityAppService : ApplicationService, IAdminUtilityAppService
    {
        private const int SearchTake = 5;

        private readonly IOrderAppService _orders;
        private readonly IProductAppService _products;
        private readonly IDashboardAppService _dashboard;
        private readonly ICustomerProfileAppService _customerProfile;

        public AdminUtilityAppService(
            IOrderAppService orders,
            IProductAppService products,
            IDashboardAppService dashboard,
            ICustomerProfileAppService customerProfile)
        {
            _orders = orders;
            _products = products;
            _dashboard = dashboard;
            _customerProfile = customerProfile;
        }

        public async Task<GlobalSearchResultDto> SearchAsync(string q)
        {
            var result = new GlobalSearchResultDto();
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return result;
            }

            var term = q.Trim();

            // Sequential awaits: all repositories in one UoW share a single DbContext,
            // so concurrent EF operations throw "A second operation was started
            // on this context instance before a previous operation completed".
            var ordersResult = await _orders.GetListAsync(new GetOrdersInput
            {
                SearchTerm = term,
                MaxResultCount = SearchTake
            });
            var productsResult = await _products.GetListAsync(new GetProductsInput
            {
                SearchTerm = term,
                MaxResultCount = SearchTake
            });
            var customersResult = await _orders.GetCustomersAsync(term);

            result.Orders = ordersResult.Items
                .Take(SearchTake)
                .Select(o => new GlobalSearchItemDto
                {
                    Type = "order",
                    Title = o.OrderNumber,
                    Subtitle = $"{o.CustomerName} • {o.CustomerPhone} • {o.Status}",
                    Link = $"/orders?search={Uri.EscapeDataString(o.OrderNumber)}"
                })
                .ToList();

            result.Products = productsResult.Items
                .Take(SearchTake)
                .Select(p => new GlobalSearchItemDto
                {
                    Type = "product",
                    Title = p.Name,
                    Subtitle = $"{p.Price:N0} ₫",
                    Link = "/products"
                })
                .ToList();

            result.Customers = (customersResult ?? new List<CustomerDto>())
                .Take(SearchTake)
                .Select(c => new GlobalSearchItemDto
                {
                    Type = "customer",
                    Title = string.IsNullOrWhiteSpace(c.CustomerName) ? c.CustomerPhone : c.CustomerName,
                    Subtitle = $"{c.CustomerPhone} • {c.OrderCount} đơn",
                    Link = $"/customers?search={Uri.EscapeDataString(c.CustomerPhone)}"
                })
                .ToList();

            return result;
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync()
        {
            var notifications = new List<NotificationDto>();
            var nowVn = DateTime.UtcNow.AddHours(7);

            // Sequential awaits (same single-DbContext reason as SearchAsync above).
            var stats = await _dashboard.GetStatsAsync();
            var reviewsResult = await _orders.GetReviewsAsync();
            var birthdaysResult = await _customerProfile.GetBirthdaysAsync(nowVn.Month);

            // 1. Stale pending orders — urgent.
            if (stats.StalePendingOrders > 0)
            {
                notifications.Add(new NotificationDto
                {
                    Id = "stale-pending",
                    Kind = "urgent",
                    Title = $"{stats.StalePendingOrders} đơn chờ xác nhận quá {stats.StalePendingMinutes} phút",
                    Detail = "Cần xử lý ngay để không trễ hẹn khách.",
                    Link = "/orders?status=Pending"
                });
            }

            // 2. Pending orders — warning.
            if (stats.PendingOrders > 0)
            {
                notifications.Add(new NotificationDto
                {
                    Id = "pending-orders",
                    Kind = "warning",
                    Title = $"{stats.PendingOrders} đơn hàng mới chưa xử lý",
                    Detail = "Nhấn để xem và xác nhận.",
                    Link = "/orders?status=Pending"
                });
            }

            // 3. Upcoming pre-orders — info.
            var upcoming = stats.UpcomingPreOrders ?? new List<OrderDto>();
            if (upcoming.Count > 0)
            {
                var next = upcoming.OrderBy(o => o.ScheduledFor).First();
                notifications.Add(new NotificationDto
                {
                    Id = "upcoming-preorder",
                    Kind = "info",
                    Title = $"{upcoming.Count} đơn đặt trước sắp đến giờ",
                    Detail = $"Gần nhất: {next.OrderNumber} lúc {next.ScheduledFor?.AddHours(7):dd/MM HH:mm}.",
                    Link = "/bep"
                });
            }

            // 4. New 1–2 star reviews (last 7 days) — warning.
            var badReviews = (reviewsResult ?? new List<OrderReviewDto>())
                .Where(r => r.Rating <= 2 && r.CreatedAt >= nowVn.AddDays(-7))
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToList();
            foreach (var r in badReviews)
            {
                notifications.Add(new NotificationDto
                {
                    Id = $"review-{r.Id}",
                    Kind = "warning",
                    Title = $"Đánh giá {r.Rating} sao từ {r.CustomerPhone}",
                    Detail = string.IsNullOrWhiteSpace(r.Comment) ? r.OrderNumber : r.Comment,
                    Link = "/admin/danh-gia"
                });
            }

            // 5. Customers with birthday today — info.
            var todayBirthdays = (birthdaysResult ?? new List<BirthdayMemberDto>())
                .Where(b => b.DateOfBirth.HasValue
                    && b.DateOfBirth.Value.Day == nowVn.Day
                    && b.DateOfBirth.Value.Month == nowVn.Month)
                .ToList();
            if (todayBirthdays.Count > 0)
            {
                notifications.Add(new NotificationDto
                {
                    Id = "birthdays-today",
                    Kind = "info",
                    Title = $"{todayBirthdays.Count} khách sinh nhật hôm nay 🎂",
                    Detail = string.Join(", ", todayBirthdays.Take(3).Select(b =>
                        string.IsNullOrWhiteSpace(b.UserName) ? b.Phone : b.UserName)),
                    Link = "/customers"
                });
            }

            return notifications;
        }
    }
}
