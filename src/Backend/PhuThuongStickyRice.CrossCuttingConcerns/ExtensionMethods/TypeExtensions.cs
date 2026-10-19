using System;
using System.Linq;

namespace PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods
{
    public static class TypeExtensions
    {
        public static Type MakeGenericTypeSafe(this Type type, params Type[] typeArguments)
        {
            return type.IsGenericType && !type.GenericTypeArguments.Any()
                ? type.MakeGenericType(typeArguments)
                : type;
        }
    }
}
