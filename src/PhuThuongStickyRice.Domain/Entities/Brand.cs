using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Nhà cung cấp/ thương hiệu
    /// </summary>
    public class Brand : Entity<Guid>, IAggregateRoot
    {
        public string Name { get; set; }
        public int Status { get; set; }
    }
}
