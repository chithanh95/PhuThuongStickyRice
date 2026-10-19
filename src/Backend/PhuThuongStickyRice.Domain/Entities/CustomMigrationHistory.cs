using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Ghi nhận các migration tùy chỉnh (viết tay, không qua `dotnet ef migrations`)
    /// đã được áp dụng vào database, để Migrator không chạy lặp lại.
    /// </summary>
    public class CustomMigrationHistory : Entity<Guid>
    {
        public string MigrationName { get; set; }
    }
}
