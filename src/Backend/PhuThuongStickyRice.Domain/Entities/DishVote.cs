using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// One vote for a dish poll (item 54). One vote per phone per poll.
    /// </summary>
    public class DishVote : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 15;

        public Guid PollId { get; set; }

        public string Phone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
