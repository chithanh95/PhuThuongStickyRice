using System.Collections.Generic;

namespace PhuThuongStickyRice.Domain.ValueObjects
{
    /// <summary>
    /// Value object địa chỉ (đường, thành phố, mã bưu chính): bất biến, so
    /// sánh theo giá trị. Dùng cho địa chỉ khách hàng, không có Id riêng.
    /// </summary>
    public class Address : ValueObject
    {
        /// <summary>Số nhà, tên đường.</summary>
        public string Street { get; private set; }

        /// <summary>Thành phố/tỉnh.</summary>
        public string City { get; private set; }

        /// <summary>Mã bưu chính.</summary>
        public string ZipCode { get; private set; }

        private Address()
        {
        }

        public Address(string street, string city, string zipCode)
        {
            Street = street;
            City = city;
            ZipCode = zipCode;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return ZipCode;
        }
    }
}
