using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    public class Product : Entity<Guid>, IAggregateRoot
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int Status { get; set; }

        public int MyProperty { get; set; }
    }
}
