using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019181000_AddPrepaidCardCardCodeIndex")]
    public partial class AddPrepaidCardCardCodeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 18:05): unique index trên
            // PrepaidCards(CardCode) — lookup CardCode là thao tác real-time
            // tại quầy (TryRedeemAsync ExecuteUpdateAsync + RedeemAsync
            // 2 lần FirstOrDefaultAsync + check trùng khi tạo thẻ), trước
            // đây toàn là clustered index scan vì bảng không có index nào.
            // Uniqueness khớp ngữ nghĩa nghiệp vụ (mã TX+6 số in hoa duy
            // nhất). Non-clustered unique, lookup-only.
            // ĐÍNH CHÍNH (fix 2026-10-19 19:10): migration
            // 20261019090000_AddPrepaidCardWeeklyPlan ĐÃ tạo index cùng tên
            // IX_PrepaidCards_CardCode (unique, filtered [CardCode] IS NOT
            // NULL). DB chạy đủ migration cũ đã có index này → CreateIndex
            // thường sẽ ném SqlException "already exists" ngay sau khi qua
            // được lỗi IX_OrderLines_OrderId. IF NOT EXISTS giữ migration an
            // toàn cho cả DB cũ lẫn DB mới; định nghĩa giữ nguyên unique +
            // filtered để khớp index đã tồn tại.
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                                 WHERE name = 'IX_PrepaidCards_CardCode'
                                   AND object_id = OBJECT_ID(N'[PrepaidCards]'))
                    CREATE UNIQUE INDEX [IX_PrepaidCards_CardCode]
                        ON [PrepaidCards] ([CardCode])
                        WHERE [CardCode] IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.indexes
                             WHERE name = 'IX_PrepaidCards_CardCode'
                               AND object_id = OBJECT_ID(N'[PrepaidCards]'))
                    DROP INDEX [IX_PrepaidCards_CardCode] ON [PrepaidCards];
                """);
        }
    }
}
