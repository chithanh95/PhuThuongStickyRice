using System;

namespace PhuThuongStickyRice.Domain.Entities.OrderAggregate
{
    public class OrderAddress : Entity<Guid>, IAggregateRoot
    {
        public string Street { get; private set; }

        public string City { get; private set; }

        public string State { get; private set; }

        public string Country { get; private set; }

        public string ZipCode { get; private set; }

#pragma warning disable CS8618 // Required by Entity Framework
        private OrderAddress() { }

        public OrderAddress(string street, string city, string state, string country, string zipcode)
        {
            Street = street;
            City = city;
            State = state;
            Country = country;
            ZipCode = zipcode;
        }
    }
}
