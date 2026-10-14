using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Saved table QR code. The encoded URL is
    /// <c>{WebBaseUrl}/shop?ban={Label}</c>.
    /// </summary>
    public class TableQr : Entity<Guid>, IAggregateRoot
    {
        public const int LabelMaxLength = 100;
        public const int UrlMaxLength = 500;

        public string Label { get; set; }

        public string EncodedUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
