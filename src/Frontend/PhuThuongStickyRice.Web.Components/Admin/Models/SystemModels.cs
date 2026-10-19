using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Admin.Models
{
    // ---------- Feature flags (item 105) ----------

    public class FeatureFlagModel
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Description { get; set; }
    }

    // ---------- Branches (item 109) ----------

    public class BranchModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // ---------- Webhooks (item 110) ----------

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

    public static class ModelEnumerableExtensions
    {
        public static IEnumerable<string> WhereNotNull(this IEnumerable<string> source)
        {
            foreach (var s in source)
            {
                if (!string.IsNullOrWhiteSpace(s))
                {
                    yield return s;
                }
            }
        }
    }

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

    public class ShortLinkModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string TargetUrl { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
    }

    // ---------- App logs (item 104) ----------

    public class AppLogModel
    {
        public Guid Id { get; set; }
        public DateTime At { get; set; }
        public string Level { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
    }

    public class PagedAppLogsModel
    {
        public long TotalCount { get; set; }
        public List<AppLogModel> Items { get; set; } = new List<AppLogModel>();
    }

    // ---------- Disk usage (item 113) ----------

    public class DiskUsageModel
    {
        public long UploadsBytes { get; set; }
        public int UploadsFileCount { get; set; }
        public long? DatabaseBytes { get; set; }
        public int WarningThresholdMb { get; set; }
        public bool OverThreshold { get; set; }
    }

    // ---------- System status (item 111) ----------

    public class SystemStatusModel
    {
        public bool MaintenanceMode { get; set; }
        public string MaintenanceMessage { get; set; }
    }

    // ---------- Address book (item 119) ----------

    public class AddressBookModel
    {
        public Guid Id { get; set; }
        public string Phone { get; set; }
        public string Label { get; set; }
        public string Address { get; set; }
    }
}
