using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Phân loại
    /// </summary>
    public class Category : Entity<Guid>, IAggregateRoot
    {
        public string Name { get; set; }
        public int Status { get; set; }
    }
}
