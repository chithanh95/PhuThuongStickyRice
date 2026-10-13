using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    public class CustomMigrationHistory : Entity<Guid>
    {
        public string MigrationName { get; set; }
    }
}
