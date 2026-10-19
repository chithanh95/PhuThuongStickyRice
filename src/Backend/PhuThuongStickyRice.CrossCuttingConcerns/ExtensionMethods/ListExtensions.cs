using System;
using System.Collections.Generic;
using System.Text;

namespace PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods
{
    public static class ListExtensions
    {
        /// <summary>Gộp <paramref name="second"/> vào cuối <paramref name="first"/> (sửa trực tiếp trên list gốc) rồi trả về chính nó.</summary>
        public static List<T> Combine<T>(this List<T> first, List<T> second)
        {
            first.AddRange(second);
            return first;
        }

        /// <summary>Tạo list mới gồm các phần tử của <paramref name="first"/> rồi đến <paramref name="second"/>; không sửa 2 collection gốc.</summary>
        public static ICollection<T> Combine<T>(this ICollection<T> first, ICollection<T> second)
        {
            var list = new List<T>();
            list.AddRange(first);
            list.AddRange(second);
            return list;
        }
    }
}
