using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019193000_AddPerfHuntIndexes19")]
    public partial class AddPerfHuntIndexes19 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 19:15): thay index đơn dư thừa
            // IX_Orders_CustomerPhoneNorm bằng composite
            // IX_Orders_CustomerPhoneNorm_CreatedAt.
            //
            // (1) Index đơn dư thừa: mọi truy vấn lọc bằng trên CustomerPhoneNorm
            // (HasOrderedBeforeAsync, EnsurePhoneDailyLimitAsync,
            // CountCompletedOrdersByPhoneAsync, HasNoPriorCompletedOrderAsync,
            // LoadOrderCandidatesAsync) đều được phục vụ qua prefix trái của
            // composite (CustomerPhoneNorm, Status) — giữ lại index đơn chỉ tốn
            // write cost + dung lượng trên bảng Orders (bảng nóng nhất), không
            // tăng tốc thêm query nào.
            //
            // (2) Composite (CustomerPhoneNorm, CreatedAt): 2 đường nóng public
            // lọc CustomerPhoneNorm == 9 số cuối + OrderByDescending(CreatedAt)
            // + Take(100) — tra cứu đơn theo SĐT (LoadOrderCandidatesAsync) và
            // prefill tên/địa chỉ checkout (GetLastOrderInfoByPhoneAsync). Trước
            // đây seek theo SĐT rồi sort trong memory; composite cho ordered
            // seek ngay trên index. Non-unique, lookup-only, không đổi nghiệp vụ.
            //
            // Raw SQL idempotent (IF EXISTS / IF NOT EXISTS) — an toàn khi DB
            // của user chưa/không có index ở trạng thái mong đợi.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Orders]') AND name = N'IX_Orders_CustomerPhoneNorm')
    DROP INDEX [IX_Orders_CustomerPhoneNorm] ON [Orders];

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Orders]') AND name = N'IX_Orders_CustomerPhoneNorm_CreatedAt')
    CREATE NONCLUSTERED INDEX [IX_Orders_CustomerPhoneNorm_CreatedAt]
        ON [Orders] ([CustomerPhoneNorm] ASC, [CreatedAt] ASC);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Orders]') AND name = N'IX_Orders_CustomerPhoneNorm_CreatedAt')
    DROP INDEX [IX_Orders_CustomerPhoneNorm_CreatedAt] ON [Orders];

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Orders]') AND name = N'IX_Orders_CustomerPhoneNorm')
    CREATE NONCLUSTERED INDEX [IX_Orders_CustomerPhoneNorm]
        ON [Orders] ([CustomerPhoneNorm] ASC);");
        }
    }
}
