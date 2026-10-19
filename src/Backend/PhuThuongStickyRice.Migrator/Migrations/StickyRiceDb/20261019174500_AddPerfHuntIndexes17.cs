using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019174500_AddPerfHuntIndexes17")]
    public partial class AddPerfHuntIndexes17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 17:25): index trên OrderLines(OrderId).
            // ĐÍNH CHÍNH (fix 2026-10-19 19:10): comment cũ nói "EF Core KHÔNG
            // tự tạo index cho FK" là SAI — EF Core có tạo index cho FK theo
            // convention, và migration 20261013174506_AddOrdering đã tạo
            // IX_OrderLines_OrderId khi tạo bảng OrderLines. DB nào chạy đủ
            // migration cũ đã có index này → CreateIndex thường ném
            // SqlException "already exists" làm Update-Database/Migrate() gãy
            // (sự cố user gặp 2026-10-19 19:04). IF NOT EXISTS giữ migration
            // an toàn cho cả DB cũ (đã có index) lẫn DB mới.
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                                 WHERE name = 'IX_OrderLines_OrderId'
                                   AND object_id = OBJECT_ID(N'[OrderLines]'))
                    CREATE INDEX [IX_OrderLines_OrderId] ON [OrderLines] ([OrderId]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.indexes
                             WHERE name = 'IX_OrderLines_OrderId'
                               AND object_id = OBJECT_ID(N'[OrderLines]'))
                    DROP INDEX [IX_OrderLines_OrderId] ON [OrderLines];
                """);
        }
    }
}
