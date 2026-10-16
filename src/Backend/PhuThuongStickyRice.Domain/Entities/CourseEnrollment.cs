using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Course enrollment keyed by phone (item 60).
    /// One enrollment per (CourseId, Phone).
    /// </summary>
    public class CourseEnrollment : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 15;

        public Guid CourseId { get; set; }

        public string Phone { get; set; }

        public DateTime CreatedAt { get; set; }

        public Course Course { get; set; }
    }
}
