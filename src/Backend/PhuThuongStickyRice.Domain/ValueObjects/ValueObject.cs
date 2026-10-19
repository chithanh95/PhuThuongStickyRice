using System;
using System.Collections.Generic;
using System.Linq;

namespace PhuThuongStickyRice.Domain.ValueObjects
{
    /// <summary>
    /// Lớp cơ sở cho mọi value object trong domain: bất biến, không có Id,
    /// bằng nhau khi mọi thành phần giá trị bằng nhau (thay vì so reference).
    /// Class con chỉ cần override <see cref="GetEqualityComponents"/> liệt kê
    /// các thành phần tham gia so sánh.
    /// </summary>
    public abstract class ValueObject
    {
        /// <summary>
        /// Liệt kê các thành phần tạo nên giá trị của object, theo thứ tự
        /// so sánh. Hai value object cùng kiểu bằng nhau khi mọi thành phần
        /// trong danh sách này bằng nhau.
        /// </summary>
        protected abstract IEnumerable<object> GetEqualityComponents();

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (GetType() != obj.GetType())
            {
                throw new ArgumentException($"Invalid comparison of Value Objects of different types: {GetType()} and {obj.GetType()}");
            }

            var valueObject = (ValueObject)obj;

            return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Aggregate(1, (current, obj) =>
                {
                    unchecked
                    {
                        return (current * 23) + (obj?.GetHashCode() ?? 0);
                    }
                });
        }

        public static bool operator ==(ValueObject a, ValueObject b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            {
                return true;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(ValueObject a, ValueObject b)
        {
            return !(a == b);
        }
    }
}
