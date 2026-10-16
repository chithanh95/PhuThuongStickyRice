using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// A candidate new dish customers can vote for (item 54).
    /// </summary>
    public class DishPoll : Entity<Guid>, IAggregateRoot
    {
        public const int DishNameMaxLength = 100;
        public const int DescriptionMaxLength = 500;
        public const int ImageUrlMaxLength = 500;

        public string DishName { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public int VoteCount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
