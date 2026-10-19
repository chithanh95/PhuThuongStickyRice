using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019121200_AddPerfHuntIndexes7")]
    public partial class AddPerfHuntIndexes7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19: admin GET webhook deliveries
            // (WebhookAppService.GetDeliveriesAsync): Where EndpointId
            // (equality, khi chọn 1 endpoint) + OrderByDescending At + Take.
            // Index hiện chỉ có At đơn (builder.HasIndex(x => x.At)) → phải
            // filter EndpointId bằng scan trên index At rồi sort lại.
            // Composite (EndpointId, At) cho seek + sort trực tiếp trên index.
            // Index non-unique: chỉ tăng tốc lookup, không đổi nghiệp vụ.
            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_EndpointId_At",
                table: "WebhookDeliveries",
                columns: new[] { "EndpointId", "At" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebhookDeliveries_EndpointId_At",
                table: "WebhookDeliveries");
        }
    }
}
