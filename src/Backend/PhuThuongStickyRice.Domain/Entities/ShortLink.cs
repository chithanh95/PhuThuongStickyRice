using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Short link (liên kết rút gọn): <c>/s/{Code}</c> 302-redirects to
    /// <see cref="TargetUrl"/>. Created from the product quick-view
    /// ("🔗 Link ngắn") or manually in <c>/admin/link-rut-gon</c>.
    /// </summary>
    public class ShortLink : Entity<Guid>, IAggregateRoot
    {
        public const int CodeMaxLength = 20;
        public const int TargetUrlMaxLength = 2000;

        /// <summary>6-character unique code.</summary>
        public string Code { get; set; }

        public string TargetUrl { get; set; }
    }
}
