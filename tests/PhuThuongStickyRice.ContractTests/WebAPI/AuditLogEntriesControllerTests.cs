using ApprovalTests;
using ApprovalTests.Reporters;
using PhuThuongStickyRice.Application.AuditLogEntries.DTOs;
using NJsonSchema;
using Xunit;

namespace PhuThuongStickyRice.ContractTests.WebAPI
{
    [UseReporter(typeof(VisualStudioReporter))]
    public class AuditLogEntriesControllerTests
    {
        [Fact]
        public void AuditLogEntryDTOTest()
        {
            var schema = JsonSchema.FromType<AuditLogEntryDTO>().ToJson();
            Approvals.Verify(schema);
        }
    }
}
