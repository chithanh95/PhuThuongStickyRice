using ApprovalTests;
using ApprovalTests.Reporters;
using PhuThuongStickyRice.WebAPI.Models.Users;
using NJsonSchema;
using Xunit;

namespace PhuThuongStickyRice.ContractTests.WebAPI
{
    [UseReporter(typeof(VisualStudioReporter))]
    public class UsersControllerTests
    {
        [Fact]
        public void UserModelTest()
        {
            var schema = JsonSchema.FromType<UserModel>().ToJson();
            Approvals.Verify(schema);
        }
    }
}
