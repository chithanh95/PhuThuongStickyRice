using System.Collections.Generic;

namespace PhuThuongStickyRice.WebAPI.Authorization
{
    public static class AuthorizationPolicyNames
    {
        public const string GetProductsPolicy = "GetProductsPolicy";
        public const string GetProductPolicy = "GetProductPolicy";
        public const string AddProductPolicy = "AddProductPolicy";
        public const string UpdateProductPolicy = "UpdateProductPolicy";
        public const string DeleteProductPolicy = "DeleteProductPolicy";

        public static IEnumerable<string> GetPolicyNames()
        {
            yield return GetProductsPolicy;
            yield return GetProductPolicy;
            yield return AddProductPolicy;
            yield return UpdateProductPolicy;
            yield return DeleteProductPolicy;
        }
    }
}
