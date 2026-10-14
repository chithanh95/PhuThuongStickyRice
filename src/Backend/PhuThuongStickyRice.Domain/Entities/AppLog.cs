using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Lightweight persisted log (nhật ký hệ thống). A logging hook
    /// writes Warning+ entries here (item 104). Pruned to the latest
    /// 2000 rows by the writer so the table never grows unbounded.
    /// </summary>
    public class AppLog : Entity<Guid>, IAggregateRoot
    {
        public const int LevelMaxLength = 20;
        public const int SourceMaxLength = 300;
        public const int MessageMaxLength = 4000;

        public DateTime At { get; set; }

        /// <summary>Information / Warning / Error / Critical.</summary>
        public string Level { get; set; }

        public string Source { get; set; }

        public string Message { get; set; }
    }
}
