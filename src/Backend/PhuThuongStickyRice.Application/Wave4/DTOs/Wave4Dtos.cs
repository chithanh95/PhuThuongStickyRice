using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Wave4.DTOs
{
    // ---------- Feature flags (item 105) ----------

    public class FeatureFlagDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Description { get; set; }
    }

    // ---------- Branches (item 109) ----------

    public class BranchDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateUpdateBranchDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        [StringLength(30)]
        public string Phone { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ---------- Webhooks (item 110) ----------

    public class WebhookEndpointDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }

        /// <summary>Masked secret — never returned to the client.</summary>
        public bool HasSecret { get; set; }
        public string Events { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateUpdateWebhookEndpointDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(1000)]
        [Url]
        public string Url { get; set; }

        /// <summary>Optional. When empty on update, the existing secret is kept.</summary>
        [StringLength(200)]
        public string Secret { get; set; }

        /// <summary>Comma-separated: OrderCreated, OrderStatusChanged.</summary>
        [StringLength(200)]
        public string Events { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class WebhookDeliveryDto
    {
        public Guid Id { get; set; }
        public DateTime At { get; set; }
        public Guid EndpointId { get; set; }
        public string EndpointName { get; set; }
        public string Event { get; set; }
        public bool Success { get; set; }
        public int? StatusCode { get; set; }
        public string Error { get; set; }
    }

    // ---------- Short links (item 116) ----------

    public class ShortLinkDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string TargetUrl { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
    }

    public class CreateShortLinkDto
    {
        [Required]
        [StringLength(2000)]
        [Url]
        public string TargetUrl { get; set; }
    }

    // ---------- Group orders (item 117) ----------

    public class GroupOrderLineDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
        public string AddedBy { get; set; }
    }

    public class GroupOrderDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string CreatorPhone { get; set; }
        public string Status { get; set; }
        public List<GroupOrderLineDto> Lines { get; set; } = new List<GroupOrderLineDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string ResultOrderNumber { get; set; }
    }

    public class CreateGroupOrderDto
    {
        [Required]
        [StringLength(30)]
        public string CreatorPhone { get; set; }
    }

    public class AddGroupOrderLineDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; }

        [StringLength(200)]
        public string Note { get; set; }

        [StringLength(100)]
        public string AddedBy { get; set; }
    }

    public class CloseGroupOrderDto
    {
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(30)]
        public string CustomerPhone { get; set; }

        [Required]
        [StringLength(500)]
        public string CustomerAddress { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }
    }

    // ---------- Address book (item 119) ----------

    public class AddressBookDto
    {
        public Guid Id { get; set; }
        public string Phone { get; set; }
        public string Label { get; set; }
        public string Address { get; set; }
    }

    public class SaveAddressDto
    {
        [Required]
        [StringLength(30)]
        public string Phone { get; set; }

        [Required]
        [StringLength(100)]
        public string Label { get; set; }

        [Required]
        [StringLength(500)]
        public string Address { get; set; }
    }

    // ---------- App logs (item 104) ----------

    public class AppLogDto
    {
        public Guid Id { get; set; }
        public DateTime At { get; set; }
        public string Level { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
    }

    public class GetAppLogsInput
    {
        public string Level { get; set; }
        public string SearchTerm { get; set; }
        public int SkipCount { get; set; }
        public int MaxResultCount { get; set; } = 50;
    }

    // ---------- System status (item 111) ----------

    public class SystemStatusDto
    {
        public bool MaintenanceMode { get; set; }
        public string MaintenanceMessage { get; set; }
    }

    // ---------- Disk usage (item 113) ----------

    public class DiskUsageDto
    {
        public long UploadsBytes { get; set; }
        public int UploadsFileCount { get; set; }
        public long? DatabaseBytes { get; set; }
        public int WarningThresholdMb { get; set; }
        public bool OverThreshold { get; set; }
    }
}
