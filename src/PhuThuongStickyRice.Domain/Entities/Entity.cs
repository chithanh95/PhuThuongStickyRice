using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Domain.Entities
{
    public abstract class Entity<TKey> : IHasKey<TKey>, ITrackable
    {
        public TKey Id { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
        public bool IsDeleted { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public DateTimeOffset? UpdatedDateTime { get; set; }

        public Guid CreatedBy { get; set; }
        public Guid? LastModifiedBy { get; set; }
    }
}
