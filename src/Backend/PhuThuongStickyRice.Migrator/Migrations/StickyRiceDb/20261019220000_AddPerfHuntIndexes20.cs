using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019220000_AddPerfHuntIndexes20")]
    public partial class AddPerfHuntIndexes20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 21:35): 3 index còn thiếu phát hiện
            // qua audit toàn bộ 115 index hiện có đối chiếu với các cột hay
            // lọc (audit của subagent, đã verify tên bảng/cột và không trùng
            // tên index với migration cũ).
            //
            // (1) IX_Products_Code — ExistsWithCodeAsync check trùng mã mỗi
            // lần admin tạo/sửa sản phẩm; trước đây full-scan bảng Products.
            // Cột Code là complex-type member (EF Core không hỗ trợ HasIndex
            // trên complex-type member) nên dùng raw SQL.
            //
            // (2) IX_AuditLogEntries_UserId — bảng audit ghi 1 dòng mỗi lần
            // tạo/sửa/xóa nên phình theo thời gian; màn admin lọc nhật ký
            // theo user bị full scan (trước đây chỉ có index trên
            // CreatedDateTime). LƯU Ý: cột ObjectId là string không giới
            // hạn độ dài → SQL Server tạo cột nvarchar(max), KHÔNG được
            // dùng làm cột khóa của index (SqlException "invalid for use
            // as a key column in an index" — đã gặp thật 2026-10-20) nên
            // không tạo index trên ObjectId; lọc theo ObjectId vẫn chạy,
            // chỉ scan thay vì seek.
            //
            // (3) IX_Subscriptions_Active_StartDate — worker sinh đơn gói tuần
            // (SubscriptionOrderWorker, 6h/lần) lọc Active + StartDate; bảng
            // chỉ có IX_Subscriptions_CreatedAt.
            //
            // Raw SQL idempotent (IF NOT EXISTS) — an toàn khi DB của user
            // chưa/không có index ở trạng thái mong đợi. Non-unique,
            // lookup-only, không đổi nghiệp vụ.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Products]') AND name = N'IX_Products_Code')
    CREATE NONCLUSTERED INDEX [IX_Products_Code]
        ON [Products] ([Code] ASC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[AuditLogEntries]') AND name = N'IX_AuditLogEntries_UserId')
    CREATE NONCLUSTERED INDEX [IX_AuditLogEntries_UserId]
        ON [AuditLogEntries] ([UserId] ASC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Subscriptions]') AND name = N'IX_Subscriptions_Active_StartDate')
    CREATE NONCLUSTERED INDEX [IX_Subscriptions_Active_StartDate]
        ON [Subscriptions] ([Active] ASC, [StartDate] ASC);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Subscriptions]') AND name = N'IX_Subscriptions_Active_StartDate')
    DROP INDEX [IX_Subscriptions_Active_StartDate] ON [Subscriptions];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[AuditLogEntries]') AND name = N'IX_AuditLogEntries_UserId')
    DROP INDEX [IX_AuditLogEntries_UserId] ON [AuditLogEntries];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Products]') AND name = N'IX_Products_Code')
    DROP INDEX [IX_Products_Code] ON [Products];");
        }
    }
}
