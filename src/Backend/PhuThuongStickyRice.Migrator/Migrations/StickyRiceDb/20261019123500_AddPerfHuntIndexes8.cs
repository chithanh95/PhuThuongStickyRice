using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019123500_AddPerfHuntIndexes8")]
    public partial class AddPerfHuntIndexes8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 12:15):
            // (1) Orders.ShipperId: admin order list (OrderAppService.GetListAsync)
            //     + order reports (OrderReportsAppService) filter
            //     Where(ShipperId == shipperId). Equality on Guid without index
            //     = table scan on the hottest table. Non-unique: lookup only,
            //     no business change.
            // NOTE: migration 20261014114805_AddContentSecurityAndRewards also
            // tried to create IX_Orders_ShipperId, but that file has no
            // [Migration] attribute so EF discovery skipped it (same root cause
            // as 2026-10-18) — the index may or may not exist depending on the
            // database. IF NOT EXISTS keeps this migration safe in both cases.
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_Orders_ShipperId'
                                 AND object_id = OBJECT_ID(N'[Orders]'))
                    CREATE INDEX [IX_Orders_ShipperId] ON [Orders] ([ShipperId]);
                """);

            // (2) EmailMessages.SentDateTime: background worker
            //     EmailMessageService.SendEmailMessagesAsync polls
            //     Where(SentDateTime == null) + OrderBy(NextAttemptDateTime)
            //     every cycle; Take(100) limits results, not the scan, and sent
            //     mail history is kept forever. Index on the leading filter
            //     column. Non-unique: lookup only, no business change.
            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_SentDateTime",
                table: "EmailMessages",
                column: "SentDateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailMessages_SentDateTime",
                table: "EmailMessages");

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Orders_ShipperId'
                             AND object_id = OBJECT_ID(N'[Orders]'))
                    DROP INDEX [IX_Orders_ShipperId] ON [Orders];
                """);
        }
    }
}
