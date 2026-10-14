using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Candidate dish on the public voting page (/binh-chon-mon-moi).
    /// Customers vote for which new dish the shop should add to the menu.
    /// </summary>
    public class DishCandidate : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;
        public const int DescriptionMaxLength = 1000;

        public string Name { get; set; }

        public string Description { get; set; }

        public long Votes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
