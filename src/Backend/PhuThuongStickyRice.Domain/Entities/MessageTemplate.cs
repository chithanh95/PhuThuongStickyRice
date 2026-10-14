using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Reusable message template (item 131): SMS / Zalo bodies with
    /// {placeholders} such as {Ten}, {MaDon}, {TongTien}.
    /// </summary>
    public class MessageTemplate : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 100;
        public const int ChannelMaxLength = 20;
        public const int BodyMaxLength = 2000;

        public string Name { get; set; }

        /// <summary>"SMS" or "Zalo".</summary>
        public string Channel { get; set; }

        /// <summary>Body with {placeholders}.</summary>
        public string Body { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
