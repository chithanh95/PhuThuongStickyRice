using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Discounts.DTOs;
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
    /// <summary>
    /// Tìm kiếm toàn cục và thông báo tổng hợp cho màn hình admin.
    /// Tìm kiếm: gõ 1 từ khóa, trả về đơn hàng + sản phẩm + khách hàng khớp.
    /// Thông báo: các cảnh báo cần xử lý (đơn chờ xác nhận quá hạn, đơn mới
    /// chưa xử lý, đơn đặt trước sắp đến giờ, đánh giá xấu, sinh nhật khách).
    /// </summary>
    public interface IAdminSearchNotificationAppService : IApplicationService
    {
        /// <summary>Tìm kiếm toàn cục trong admin theo từ khoá: đơn hàng, sản phẩm, khách hàng.</summary>
        /// <param name="query">Từ khóa tìm kiếm (ít nhất 2 ký tự sau khi trim).</param>
        /// <returns>Kết quả tìm kiếm theo 3 nhóm: đơn hàng, sản phẩm, khách hàng.</returns>
        Task<GlobalSearchResultDto> SearchAsync(string query);

        /// <summary>Item 158: aggregated admin notifications.</summary>
        /// <returns>Danh sách thông báo cần xử lý, sắp xếp khẩn cấp trước.</returns>
        Task<List<NotificationDto>> GetNotificationsAsync();
    }

    /// <summary>
    /// Item 158: tìm kiếm toàn cục và thông báo tổng hợp cho admin.
    /// Search chạy trên 3 nguồn (đơn/sản phẩm/khách), mỗi nguồn tối đa 5 kết quả
    /// để dropdown topbar gọn. Notifications được dựng từ dashboard stats +
    /// đánh giá + sinh nhật khách, dùng cấp độ khẩn (urgent/warning/info).
    /// </summary>
    public class AdminSearchNotificationAppService : ApplicationService, IAdminSearchNotificationAppService
    {
        /// <summary>Số kết quả tối đa mỗi nguồn (đơn/sản phẩm/khách) trong dropdown tìm kiếm — giữ dropdown topbar gọn, không tràn.</summary>
        private const int SearchTake = 5;

        /// <summary>How far back to look for low-star reviews.</summary>
        private static readonly TimeSpan BadReviewLookback = TimeSpan.FromDays(7);

        /// <summary>Max low-star reviews surfaced per notification batch.</summary>
        private const int BadReviewTake = 5;

        /// <summary>Max birthday customer names shown in the notification detail.</summary>
        private const int BirthdayNamesTake = 3;

        private readonly IOrderAppService _orders;
        private readonly IProductAppService _products;
        private readonly IDashboardAppService _dashboard;
        private readonly ICustomerProfileAppService _customerProfile;
        private readonly IDiscountAppService _discounts;

        /// <summary>
        /// Khởi tạo service tìm kiếm và thông báo admin.
        /// </summary>
        /// <param name="orders">Service đơn hàng (tìm đơn, lấy đánh giá).</param>
        /// <param name="products">Service sản phẩm (tìm sản phẩm).</param>
        /// <param name="dashboard">Service dashboard (số liệu cho thông báo).</param>
        /// <param name="customerProfile">Service khách hàng (tìm khách, sinh nhật).</param>
        /// <param name="discounts">Service mã giảm giá (item 252: mã hết lượt).</param>
        public AdminSearchNotificationAppService(
            IOrderAppService orders,
            IProductAppService products,
            IDashboardAppService dashboard,
            ICustomerProfileAppService customerProfile,
            IDiscountAppService discounts)
        {
            _orders = orders;
            _products = products;
            _dashboard = dashboard;
            _customerProfile = customerProfile;
            _discounts = discounts;
        }

        /// <summary>
        /// Tìm kiếm toàn cục: chạy song song logic trên 3 nguồn (đơn hàng,
        /// sản phẩm, khách hàng) rồi map thành item có link tới màn hình
        /// tương ứng. Từ khóa dưới 2 ký tự trả về rỗng để tránh quét cả bảng.
        /// </summary>
        /// <param name="query">Từ khóa tìm kiếm (ít nhất 2 ký tự sau khi trim).</param>
        /// <returns>Kết quả tìm kiếm theo 3 nhóm: đơn hàng, sản phẩm, khách hàng.</returns>
        public virtual async Task<GlobalSearchResultDto> SearchAsync(string query)
        {
            var result = new GlobalSearchResultDto();
            // Từ khóa quá ngắn (1 ký tự) sẽ match gần như toàn bảng — chặn sớm
            // để vừa nhẹ DB vừa tránh trả về hàng nghìn kết quả vô nghĩa.
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return result;
            }

            var term = query.Trim();

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

            result.Orders = MapOrderSearchItems(ordersResult.Items);
            result.Products = MapProductSearchItems(productsResult.Items);
            result.Customers = MapCustomerSearchItems(customersResult);

            return result;
        }

        /// <summary>Map đơn hàng tìm được sang item kết quả search (link tới màn đơn hàng).</summary>
        private static List<GlobalSearchItemDto> MapOrderSearchItems(IReadOnlyList<OrderDto> orders)
        {
            return orders
                .Take(SearchTake)
                .Select(o => new GlobalSearchItemDto
                {
                    Type = "order",
                    Title = o.OrderNumber,
                    Subtitle = $"{o.CustomerName} • {o.CustomerPhone} • {o.Status}",
                    Link = $"/orders?search={Uri.EscapeDataString(o.OrderNumber)}"
                })
                .ToList();
        }

        /// <summary>Map sản phẩm tìm được sang item kết quả search (kèm giá bán).</summary>
        private static List<GlobalSearchItemDto> MapProductSearchItems(IReadOnlyList<ProductDto> products)
        {
            return products
                .Take(SearchTake)
                .Select(p => new GlobalSearchItemDto
                {
                    Type = "product",
                    Title = p.Name,
                    Subtitle = $"{p.Price:N0} ₫",
                    Link = "/products"
                })
                .ToList();
        }

        /// <summary>Map khách hàng tìm được sang item kết quả search (link tới màn khách hàng).</summary>
        private static List<GlobalSearchItemDto> MapCustomerSearchItems(List<CustomerDto> customers)
        {
            return (customers ?? new List<CustomerDto>())
                .Take(SearchTake)
                .Select(c => new GlobalSearchItemDto
                {
                    Type = "customer",
                    Title = string.IsNullOrWhiteSpace(c.CustomerName) ? c.CustomerPhone : c.CustomerName,
                    Subtitle = $"{c.CustomerPhone} • {c.OrderCount} đơn",
                    Link = $"/customers?search={Uri.EscapeDataString(c.CustomerPhone)}"
                })
                .ToList();
        }

        /// <summary>
        /// Dựng danh sách thông báo tổng hợp cho chuông topbar admin: đơn chờ
        /// xác nhận quá hạn (urgent), đơn mới chưa xử lý (warning), đơn đặt
        /// trước sắp đến giờ (info), đánh giá 1–2 sao trong 7 ngày (warning),
        /// sinh nhật khách hôm nay (info). Các ngưỡng (phút quá hạn, số ngày
        /// nhìn lại đánh giá) lấy từ DashboardStatsDto và hằng số của class.
        /// </summary>
        /// <returns>Danh sách thông báo, khẩn cấp trước.</returns>
        public virtual async Task<List<NotificationDto>> GetNotificationsAsync()
        {
            var notifications = new List<NotificationDto>();
            var nowVn = VietnamTime.ToVietnamTime(DateTime.UtcNow);

            // Sequential awaits (same single-DbContext reason as SearchAsync above).
            var stats = await _dashboard.GetStatsAsync();
            var reviewsResult = await _orders.GetReviewsAsync();
            var birthdaysResult = await _customerProfile.GetBirthdaysAsync(nowVn.Month);
            var exhaustedCodes = await _discounts.GetExhaustedCodesAsync();

            AddStalePendingNotification(notifications, stats);
            AddPendingOrdersNotification(notifications, stats);
            AddUpcomingPreOrderNotification(notifications, stats);
            AddBadReviewNotifications(notifications, reviewsResult, nowVn);
            AddBirthdayNotification(notifications, birthdaysResult, nowVn);
            AddExhaustedDiscountNotifications(notifications, exhaustedCodes);

            return notifications;
        }

        /// <summary>
        /// Item 252: mã giảm giá bị hệ thống tự tạm dừng vì dùng hết lượt —
        /// warning để admin nạp thêm ngân sách hoặc tạo mã mới.
        /// </summary>
        private static void AddExhaustedDiscountNotifications(
            List<NotificationDto> notifications,
            List<DiscountCodeDto> exhaustedCodes)
        {
            var codes = exhaustedCodes ?? new List<DiscountCodeDto>();
            if (codes.Count == 0)
            {
                return;
            }

            var shown = codes.Take(3).Select(x => x.Code);
            notifications.Add(new NotificationDto
            {
                Id = "exhausted-discounts",
                Kind = "warning",
                Title = $"{codes.Count} mã giảm giá đã tự tạm dừng vì hết lượt dùng",
                Detail = "Các mã: " + string.Join(", ", shown)
                    + ". Tăng giới hạn lượt dùng hoặc tạo mã mới.",
                Link = "/admin/khuyen-mai"
            });
        }

        /// <summary>Stale pending orders — urgent.</summary>
        private static void AddStalePendingNotification(
            List<NotificationDto> notifications, DashboardStatsDto stats)
        {
            if (stats.StalePendingOrders <= 0)
            {
                return;
            }

            notifications.Add(new NotificationDto
            {
                Id = "stale-pending",
                Kind = "urgent",
                Title = $"{stats.StalePendingOrders} đơn chờ xác nhận quá {stats.StalePendingMinutes} phút",
                Detail = "Cần xử lý ngay để không trễ hẹn khách.",
                Link = "/orders?status=Pending"
            });
        }

        /// <summary>Pending orders — warning.</summary>
        private static void AddPendingOrdersNotification(
            List<NotificationDto> notifications, DashboardStatsDto stats)
        {
            if (stats.PendingOrders <= 0)
            {
                return;
            }

            notifications.Add(new NotificationDto
            {
                Id = "pending-orders",
                Kind = "warning",
                Title = $"{stats.PendingOrders} đơn hàng mới chưa xử lý",
                Detail = "Nhấn để xem và xác nhận.",
                Link = "/orders?status=Pending"
            });
        }

        /// <summary>Upcoming pre-orders — info.</summary>
        private static void AddUpcomingPreOrderNotification(
            List<NotificationDto> notifications, DashboardStatsDto stats)
        {
            var upcoming = stats.UpcomingPreOrders ?? new List<OrderDto>();
            if (upcoming.Count == 0)
            {
                return;
            }

            var next = upcoming.MinBy(o => o.ScheduledFor);
            var scheduledVn = next.ScheduledFor.HasValue
                ? VietnamTime.ToVietnamTime(next.ScheduledFor.Value).ToString("dd/MM HH:mm")
                : null;
            notifications.Add(new NotificationDto
            {
                Id = "upcoming-preorder",
                Kind = "info",
                Title = $"{upcoming.Count} đơn đặt trước sắp đến giờ",
                Detail = $"Gần nhất: {next.OrderNumber} lúc {scheduledVn}.",
                Link = "/bep"
            });
        }

        /// <summary>New 1–2 star reviews (last 7 days) — warning.</summary>
        private static void AddBadReviewNotifications(
            List<NotificationDto> notifications,
            List<OrderReviewDto> reviewsResult,
            DateTime nowVn)
        {
            var cutoff = nowVn.Subtract(BadReviewLookback);
            var badReviews = (reviewsResult ?? new List<OrderReviewDto>())
                .Where(r => r.Rating <= 2 && r.CreatedAt >= cutoff)
                .OrderByDescending(r => r.CreatedAt)
                .Take(BadReviewTake)
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
        }

        /// <summary>Customers with birthday today — info.</summary>
        private static void AddBirthdayNotification(
            List<NotificationDto> notifications,
            List<BirthdayMemberDto> birthdaysResult,
            DateTime nowVn)
        {
            var todayBirthdays = (birthdaysResult ?? new List<BirthdayMemberDto>())
                .Where(b => b.DateOfBirth.HasValue
                    && b.DateOfBirth.Value.Day == nowVn.Day
                    && b.DateOfBirth.Value.Month == nowVn.Month)
                .ToList();
            if (todayBirthdays.Count == 0)
            {
                return;
            }

            notifications.Add(new NotificationDto
            {
                Id = "birthdays-today",
                Kind = "info",
                Title = $"{todayBirthdays.Count} khách sinh nhật hôm nay 🎂",
                Detail = string.Join(", ", todayBirthdays.Take(BirthdayNamesTake).Select(b =>
                    string.IsNullOrWhiteSpace(b.UserName) ? b.Phone : b.UserName)),
                Link = "/customers"
            });
        }
    }
}
