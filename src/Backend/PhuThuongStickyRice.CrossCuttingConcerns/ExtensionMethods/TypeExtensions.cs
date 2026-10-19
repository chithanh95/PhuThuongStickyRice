using System;

namespace PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods
{
    public static class TypeExtensions
    {
        public static Type MakeGenericTypeSafe(this Type type, params Type[] typeArguments)
        {
            // CA1860: với mảng (Type[]) dùng Length thay vì Any() — nhanh hơn và rõ hơn.
            return type.IsGenericType && type.GenericTypeArguments.Length == 0
                ? type.MakeGenericType(typeArguments)
                : type;
        }
    }
}
