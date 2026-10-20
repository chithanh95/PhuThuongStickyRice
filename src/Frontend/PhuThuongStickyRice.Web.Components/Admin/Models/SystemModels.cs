using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Admin.Models
{
    // ---------- Feature flags (item 105) ----------

    /// <summary>
    /// Cờ tính năng (feature flag) cho phép bật/tắt một chức năng trên web
    /// mà không cần deploy lại code (item 105).
    /// </summary>
    public class FeatureFlagModel
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Description { get; set; }
    }

    // ---------- Branches (item 109) ----------

    /// <summary>
    /// Thông tin chi nhánh/quầy bán xôi (item 109).
    /// </summary>
    public class BranchModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // ---------- Webhooks (item 110) ----------

    /// <summary>
    /// Endpoint nhận webhook sự kiện đơn hàng (item 110).
    /// EventsCsv tự tổng hợp từ các checkbox đăng ký sự kiện, HasSecret cho biết
    /// endpoint có cấu hình chữ ký xác thực mà không cần hiện secret thật ra UI.
    /// </summary>
    public class WebhookEndpointModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public bool HasSecret { get; set; }
        public string Secret { get; set; }
        public string Events { get; set; }
        public bool IsActive { get; set; } = true;

        public bool WantsOrderCreated { get; set; } = true;
        public bool WantsOrderStatusChanged { get; set; } = true;

        public string EventsCsv =>
            string.Join(",", new[]
            {
                WantsOrderCreated ? "OrderCreated" : null,
                WantsOrderStatusChanged ? "OrderStatusChanged" : null
            }.WhereNotNull());
    }

    /// <summary>
    /// Helper lọc bỏ các phần tử null/rỗng trước khi ghép thành chuỗi sự kiện,
    /// dùng khi tổng hợp EventsCsv từ các checkbox đăng ký webhook.
    /// </summary>
    public static class ModelEnumerableExtensions
    {
    /// <summary>
    /// Lọc bỏ chuỗi null, rỗng hoặc chỉ khoảng trắng khỏi danh sách.
    /// </summary>
    /// <param name="source">Danh sách chuỗi nguồn, có thể chứa phần tử null.</param>
    /// <returns>Các chuỗi có nội dung thật.</returns>
        public static IEnumerable<string> WhereNotNull(this IEnumerable<string> source)
        {
            foreach (var item in source)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    yield return item;
                }
            }
        }
    }

    /// <summary>
    /// Một lần gửi webhook (item 110): thời gian gửi, sự kiện, HTTP status
    /// và lỗi nếu gửi thất bại.
    /// </summary>
    public class WebhookDeliveryModel
    {
        public Guid Id { get; set; }
        public DateTime At { get; set; }
        public Guid EndpointId { get; set; }
        public string EndpointName { get; set; }
        public string Event { get; set; }
        public bool IsSuccess { get; set; }
        public int? StatusCode { get; set; }
        public string Error { get; set; }
    }

    // ---------- Short links (item 116) ----------

    /// <summary>
    /// Link rút gọn (item 116): mã ngắn trỏ tới URL đích để gửi cho khách qua tin nhắn.
    /// </summary>
    public class ShortLinkModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string TargetUrl { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
    }

    // ---------- App logs (item 104) ----------

    /// <summary>
    /// Một bản ghi log ứng dụng hiển thị trên trang giám sát hệ thống (item 104).
    /// </summary>
    public class AppLogModel
    {
        public Guid Id { get; set; }
        public DateTime At { get; set; }
        public string Level { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Kết quả phân trang danh sách log ứng dụng: tổng số bản ghi và trang hiện tại (item 104).
    /// </summary>
    public class PagedAppLogsModel
    {
        public long TotalCount { get; set; }
        public List<AppLogModel> Items { get; set; } = new List<AppLogModel>();
    }

    // ---------- Disk usage (item 113) ----------

    /// <summary>
    /// Thống kê dung lượng ổ đĩa (item 113): thư mục upload ảnh và database,
    /// cảnh báo khi vượt ngưỡng MB cấu hình.
    /// </summary>
    public class DiskUsageModel
    {
        public long UploadsBytes { get; set; }
        public int UploadsFileCount { get; set; }
        public long? DatabaseBytes { get; set; }
        public int WarningThresholdMb { get; set; }
        public bool OverThreshold { get; set; }
    }

    // ---------- System status (item 111) ----------

    /// <summary>
    /// Trạng thái hệ thống (item 111): chế độ bảo trì để tạm khóa web khi nâng cấp,
    /// kèm thông báo hiển thị cho khách.
    /// </summary>
    public class SystemStatusModel
    {
        public bool MaintenanceMode { get; set; }
        public string MaintenanceMessage { get; set; }
    }

    // ---------- Address book (item 119) ----------

    /// <summary>
    /// Một địa chỉ giao hàng đã lưu trong sổ địa chỉ của quán (item 119),
    /// tra nhanh bằng số điện thoại khi tạo đơn.
    /// </summary>
    public class AddressBookModel
    {
        public Guid Id { get; set; }
        public string Phone { get; set; }
        public string Label { get; set; }
        public string Address { get; set; }
    }
}
