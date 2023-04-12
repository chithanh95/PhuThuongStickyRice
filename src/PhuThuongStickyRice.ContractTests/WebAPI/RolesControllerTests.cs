using ApprovalTests;
using ApprovalTests.Reporters;
using PhuThuongStickyRice.WebAPI.Models.Roles;
using NJsonSchema;
using Xunit;

namespace PhuThuongStickyRice.ContractTests.WebAPI
{
    [UseReporter(typeof(VisualStudioReporter))]
    public class RolesControllerTests
    {
        [Fact]
        public void RoleModelTest()
        {
            var schema = JsonSchema.FromType<RoleModel>().ToJson();
            Approvals.Verify(schema);
        }
    }
}
