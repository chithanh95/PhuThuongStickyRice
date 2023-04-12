using ApprovalTests;
using ApprovalTests.Reporters;
using PhuThuongStickyRice.WebAPI.Models.Products;
using NJsonSchema;
using Xunit;

namespace PhuThuongStickyRice.ContractTests.WebAPI
{
    [UseReporter(typeof(VisualStudioReporter))]
    public class ProductsControllerTests
    {
        [Fact]
        public void ProductModelTest()
        {
            var schema = JsonSchema.FromType<ProductModel>().ToJson();
            Approvals.Verify(schema);
        }
    }
}
