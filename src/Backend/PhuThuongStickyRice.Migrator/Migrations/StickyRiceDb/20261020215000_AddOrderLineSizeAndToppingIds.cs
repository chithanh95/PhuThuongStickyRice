using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261020215000_AddOrderLineSizeAndToppingIds")]
    public partial class AddOrderLineSizeAndToppingIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // S25 (reorder mất lựa chọn gốc): OrderLines trước đây chỉ lưu
            // TÊN size/topping (SizeName/ToppingNames) — luồng "đặt lại"
            // (TaiKhoan/TrackOrder) không có id nên phải bỏ lựa chọn hoặc
            // đoán lại theo tên, tên bị đổi là khôi phục sai món.
            // Thêm 2 cột nullable lưu id tại thời điểm đặt:
            //   SizeOptionId (uniqueidentifier) — id size đã chọn.
            //   ToppingIds (nvarchar(1000)) — CSV các Guid topping đã chọn.
            // Đơn cũ giữ NULL (frontend fallback đoán theo tên như trước).
            // Không FK cho SizeOptionId: cùng quy ước ProductId (dòng combo
            // tham chiếu id không thuộc bảng Products) và tránh gãy khi size
            // bị xóa khỏi catalog sau này.
            //
            // Raw SQL có guard IF NOT EXISTS (theo bài học migration trùng
            // của project) — chạy lại an toàn, không sập Update-Database.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[OrderLines]') AND name = N'SizeOptionId')
    ALTER TABLE [OrderLines] ADD [SizeOptionId] uniqueidentifier NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[OrderLines]') AND name = N'ToppingIds')
    ALTER TABLE [OrderLines] ADD [ToppingIds] nvarchar(1000) NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // DROP COLUMN IF EXISTS (SQL Server 2016+) — an toàn khi cột
            // chưa từng được thêm (DB ở trạng thái lạ).
            migrationBuilder.Sql(@"
ALTER TABLE [OrderLines] DROP COLUMN IF EXISTS [ToppingIds];
ALTER TABLE [OrderLines] DROP COLUMN IF EXISTS [SizeOptionId];
");
        }
    }
}
