using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Audit trail of who changed a product and what changed (item 148).
    /// One row per changed field per save — only fields that actually
    /// changed are recorded. OldValue/NewValue are display strings.
    /// </summary>
    public class ProductChangeLog : Entity<Guid>, IAggregateRoot
    {
        public const int FieldNameMaxLength = 100;
        public const int ValueMaxLength = 500;
        public const int ChangedByMaxLength = 100;

        public Guid ProductId { get; set; }

        public string ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Vietnamese field label, e.g. "Giá bán".</summary>
        public string FieldName { get; set; }

        public string OldValue { get; set; }

        public string NewValue { get; set; }
    }
}
